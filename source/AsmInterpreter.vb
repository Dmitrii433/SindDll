' ─────────────────────────────────────────────────────────────────────────────
'  AsmInterpreter.vb
'  Namespace Interpreter — CPU, интерпретатор x86 ASM, результат шага.
' ─────────────────────────────────────────────────────────────────────────────

Imports System.Collections.Generic
Imports System.Globalization
Imports System.Text

Namespace Interpreter

    ''' <summary>Состояние регистров и флагов x86 (32-бит).</summary>
    Public Class AsmCpu

        Public EAX As UInteger
        Public EBX As UInteger
        Public ECX As UInteger
        Public EDX As UInteger
        Public ESI As UInteger
        Public EDI As UInteger
        Public EBP As UInteger
        Public ESP As UInteger
        Public EIP As UInteger

        Public ZF As Boolean        ' Zero
        Public SF As Boolean        ' Sign
        Public CF As Boolean        ' Carry
        Public OFlag As Boolean     ' Overflow  (OF — ключевое слово VB, поэтому OFlag)

        Public Sub Reset()
            EAX = 0UI : EBX = 0UI : ECX = 0UI : EDX = 0UI
            ESI = 0UI : EDI = 0UI : EBP = 0UI : ESP = 0UI : EIP = 0UI
            ZF = False : SF = False : CF = False : OFlag = False
        End Sub

        Public Function Snapshot() As Dictionary(Of String, String)
            Return New Dictionary(Of String, String) From {
                {"eax", $"0x{EAX:X8}"}, {"ebx", $"0x{EBX:X8}"},
                {"ecx", $"0x{ECX:X8}"}, {"edx", $"0x{EDX:X8}"},
                {"esi", $"0x{ESI:X8}"}, {"edi", $"0x{EDI:X8}"},
                {"ebp", $"0x{EBP:X8}"}, {"esp", $"0x{ESP:X8}"},
                {"eip", $"0x{EIP:X8}"},
                {"zf", If(ZF, "1", "0")}, {"sf", If(SF, "1", "0")},
                {"cf", If(CF, "1", "0")}, {"of", If(OFlag, "1", "0")}
            }
        End Function
    End Class

    ''' <summary>Результат выполнения одной инструкции.</summary>
    Public Class AsmStepResult
        Public Property Ok As Boolean
        Public Property ErrorMessage As String
        Public Property Jumped As Boolean
        Public Property Halted As Boolean
        Public Property Instruction As String
    End Class

    ''' <summary>Интерпретатор x86 ASM.</summary>
    Public Class AsmInterpreter

        Private _code As New List(Of String)()
        Private _pc As Integer = 0
        Private _stack As New Stack(Of UInteger)()
        Private _memory As New Dictionary(Of UInteger, UInteger)()

        Public ReadOnly Property CPU As New AsmCpu()

        Public ReadOnly Property ProgramCounter As Integer
            Get
                Return _pc
            End Get
        End Property

        Public ReadOnly Property Halted As Boolean
            Get
                Return _pc < 0 OrElse _pc >= _code.Count
            End Get
        End Property

        Public ReadOnly Property CodeCount As Integer
            Get
                Return _code.Count
            End Get
        End Property

        Public ReadOnly Property Memory As IReadOnlyDictionary(Of UInteger, UInteger)
            Get
                Return _memory
            End Get
        End Property

        ' ─────────────────────────── Загрузка ───────────────────────────

        Public Sub Load(code As NativeArray(Of NativeString))
            _code.Clear()
            If code IsNot Nothing Then
                For i As Integer = 0 To code.Length - 1
                    _code.Add(If(code(i)?.ToString(), String.Empty))
                Next
            End If
            Reset()
        End Sub

        Public Sub Load(code As String())
            _code.Clear()
            If code IsNot Nothing Then _code.AddRange(code)
            Reset()
        End Sub

        Public Sub Reset()
            _pc = 0
            _stack.Clear()
            _memory.Clear()
            CPU.Reset()
        End Sub

        ' ─────────────────────────── Выполнение ───────────────────────────

        Public Function StepOnce() As AsmStepResult
            Dim res As New AsmStepResult()
            If Halted Then
                res.Halted = True
                Return res
            End If

            Dim line As String = _code(_pc).Trim()
            res.Instruction = line
            CPU.EIP = CUInt(_pc)

            If line.Length = 0 OrElse line.StartsWith(";") OrElse line.StartsWith("//") Then
                _pc += 1
                res.Ok = True
                Return res
            End If

            Try
                Dim jumped As Boolean = ExecuteLine(line)
                res.Jumped = jumped
                If Not jumped Then _pc += 1
                res.Ok = True
                res.Halted = Halted
            Catch ex As Exception
                res.Ok = False
                res.ErrorMessage = ex.Message
            End Try

            Return res
        End Function

        Public Function RunUntil(breakpoints As IEnumerable(Of Integer)) As AsmStepResult
            Dim bps As New HashSet(Of Integer)(breakpoints)
            Dim last As New AsmStepResult()

            Do While Not Halted
                If bps.Contains(_pc) AndAlso _pc > 0 Then
                    last.Ok = True
                    last.Halted = False
                    Return last
                End If

                last = StepOnce()
                If Not last.Ok Then Return last
            Loop

            last.Halted = True
            Return last
        End Function

        ' ─────────────────────────── Исполнитель ───────────────────────────

        Private Function ExecuteLine(raw As String) As Boolean
            Dim line As String = StripComment(raw).Trim()

            Dim mnemonic As String = String.Empty
            Dim operands As String() = Array.Empty(Of String)()
            ParseInstruction(line, mnemonic, operands)

            Select Case mnemonic.ToLowerInvariant()
                Case "nop" : Return False
                Case "int3" : Throw New InvalidOperationException("INT3 (breakpoint)")
                Case "mov" : DoMov(operands) : Return False
                Case "add" : DoArith(operands, AddressOf AddOp) : Return False
                Case "sub" : DoArith(operands, AddressOf SubOp) : Return False
                Case "cmp" : DoArith(operands, AddressOf CmpOp) : Return False
                Case "inc" : DoUnary(operands, Function(v) (v + 1UI) And &HFFFFFFFFUI) : Return False
                Case "dec" : DoUnary(operands, Function(v) (v - 1UI) And &HFFFFFFFFUI) : Return False
                Case "xor" : DoArith(operands, AddressOf XorOp) : Return False
                Case "and" : DoArith(operands, AddressOf AndOp) : Return False
                Case "or" : DoArith(operands, AddressOf OrOp) : Return False
                Case "shl" : DoShift(operands, True) : Return False
                Case "shr" : DoShift(operands, False) : Return False
                Case "imul" : DoArith(operands, AddressOf IMulOp) : Return False
                Case "push" : _stack.Push(ReadOperand(operands(0))) : Return False
                Case "pop" : WriteOperand(operands(0), _stack.Pop()) : Return False

                Case "call"
                    _stack.Push(CUInt(_pc + 1))
                    Return JumpTo(operands(0))

                Case "ret"
                    If _stack.Count > 0 Then
                        _pc = CInt(_stack.Pop())
                        Return True
                    Else
                        _pc = _code.Count
                        Return True
                    End If

                Case "jmp" : Return JumpTo(operands(0))
                Case "je", "jz" : If CPU.ZF Then Return JumpTo(operands(0)) Else Return False
                Case "jne", "jnz" : If Not CPU.ZF Then Return JumpTo(operands(0)) Else Return False
                Case "jg", "jnle" : If Not CPU.ZF AndAlso (CPU.SF = CPU.OFlag) Then Return JumpTo(operands(0)) Else Return False
                Case "jl", "jnge" : If CPU.SF <> CPU.OFlag Then Return JumpTo(operands(0)) Else Return False
                Case "jge" : If CPU.SF = CPU.OFlag Then Return JumpTo(operands(0)) Else Return False
                Case "jle" : If CPU.ZF OrElse (CPU.SF <> CPU.OFlag) Then Return JumpTo(operands(0)) Else Return False
                Case "ja" : If Not CPU.CF AndAlso Not CPU.ZF Then Return JumpTo(operands(0)) Else Return False
                Case "jb" : If CPU.CF Then Return JumpTo(operands(0)) Else Return False
                Case "hlt" : _pc = _code.Count : Return True
                Case Else
                    Throw New NotSupportedException($"Неизвестная инструкция: {mnemonic}")
            End Select
        End Function

        ' ─────────────────────────── Арифметика ───────────────────────────

        Private Sub DoMov(ops As String())
            If ops.Length <> 2 Then Throw New FormatException("mov требует 2 операнда")
            WriteOperand(ops(0), ReadOperand(ops(1)))
        End Sub

        Private Delegate Function BinOp(a As UInteger, b As UInteger) As UInteger

        Private Sub DoArith(ops As String(), op As BinOp)
            If ops.Length <> 2 Then Throw New FormatException("инструкция требует 2 операнда")
            Dim dst = ReadOperand(ops(0))
            Dim src = ReadOperand(ops(1))
            Dim r = op(dst, src)
            WriteOperand(ops(0), r)
        End Sub

        Private Function AddOp(a As UInteger, b As UInteger) As UInteger
            Dim r = a + b
            CPU.CF = r > &HFFFFFFFFUI
            Dim res = r And &HFFFFFFFFUI
            SetFlags(res)
            CPU.OFlag = ((a Xor res) And (b Xor res) And &H80000000UI) <> 0
            Return res
        End Function

        Private Function SubOp(a As UInteger, b As UInteger) As UInteger
            Dim r = a - b
            CPU.CF = a < b
            Dim res = r And &HFFFFFFFFUI
            SetFlags(res)
            CPU.OFlag = ((a Xor b) And (a Xor res) And &H80000000UI) <> 0
            Return res
        End Function

        Private Function CmpOp(a As UInteger, b As UInteger) As UInteger
            SubOp(a, b)
            Return a
        End Function

        Private Function XorOp(a As UInteger, b As UInteger) As UInteger
            Dim res = a Xor b
            SetFlags(res) : CPU.CF = False : CPU.OFlag = False
            Return res
        End Function

        Private Function AndOp(a As UInteger, b As UInteger) As UInteger
            Dim res = a And b
            SetFlags(res) : CPU.CF = False : CPU.OFlag = False
            Return res
        End Function

        Private Function OrOp(a As UInteger, b As UInteger) As UInteger
            Dim res = a Or b
            SetFlags(res) : CPU.CF = False : CPU.OFlag = False
            Return res
        End Function

        Private Function IMulOp(a As UInteger, b As UInteger) As UInteger
            Dim r = CLng(a) * CLng(b)
            Dim res = CUInt(r And &HFFFFFFFFL)
            SetFlags(res)
            Return res
        End Function

        Private Sub SetFlags(res As UInteger)
            CPU.ZF = (res = 0UI)
            CPU.SF = (res And &H80000000UI) <> 0
        End Sub

        Private Sub DoUnary(ops As String(), op As Func(Of UInteger, UInteger))
            If ops.Length <> 1 Then Throw New FormatException("инструкция требует 1 операнд")
            Dim v = ReadOperand(ops(0))
            Dim r = op(v)
            WriteOperand(ops(0), r)
            SetFlags(r)
        End Sub

        Private Sub DoShift(ops As String(), left As Boolean)
            If ops.Length <> 2 Then Throw New FormatException("shift требует 2 операнда")
            Dim dst = ReadOperand(ops(0))
            Dim cnt = CInt(ReadOperand(ops(1)) And 31UI)
            Dim r As UInteger
            If left Then r = (dst << cnt) And &HFFFFFFFFUI Else r = dst >> cnt
            WriteOperand(ops(0), r)
            SetFlags(r)
        End Sub

        ' ─────────────────────────── Операнды ───────────────────────────

        Private Function ReadOperand(op As String) As UInteger
            op = op.Trim()
            If op.Length = 0 Then Throw New FormatException("Пустой операнд")

            If op.StartsWith("[") AndAlso op.EndsWith("]") Then
                Dim addr = ReadOperand(op.Substring(1, op.Length - 2))
                Dim v As UInteger
                If _memory.TryGetValue(addr, v) Then Return v
                Return 0UI
            End If

            Dim num As UInteger
            If TryParseNumber(op, num) Then Return num

            Select Case op.ToLowerInvariant()
                Case "eax" : Return CPU.EAX
                Case "ebx" : Return CPU.EBX
                Case "ecx" : Return CPU.ECX
                Case "edx" : Return CPU.EDX
                Case "esi" : Return CPU.ESI
                Case "edi" : Return CPU.EDI
                Case "ebp" : Return CPU.EBP
                Case "esp" : Return CPU.ESP
                Case "eip" : Return CPU.EIP
            End Select

            Throw New FormatException($"Неизвестный операнд: {op}")
        End Function

        Private Sub WriteOperand(op As String, value As UInteger)
            op = op.Trim()

            If op.StartsWith("[") AndAlso op.EndsWith("]") Then
                Dim addr = ReadOperand(op.Substring(1, op.Length - 2))
                _memory(addr) = value
                Return
            End If

            Select Case op.ToLowerInvariant()
                Case "eax" : CPU.EAX = value
                Case "ebx" : CPU.EBX = value
                Case "ecx" : CPU.ECX = value
                Case "edx" : CPU.EDX = value
                Case "esi" : CPU.ESI = value
                Case "edi" : CPU.EDI = value
                Case "ebp" : CPU.EBP = value
                Case "esp" : CPU.ESP = value
                Case "eip" : CPU.EIP = value
                Case Else
                    Throw New FormatException($"Нельзя записать в операнд: {op}")
            End Select
        End Sub

        Private Function TryParseNumber(s As String, ByRef result As UInteger) As Boolean
            s = s.Trim()
            If s.StartsWith("-") Then
                Dim neg As UInteger
                If TryParseNumber(s.Substring(1), neg) Then
                    result = (0UI - neg) And &HFFFFFFFFUI
                    Return True
                End If
                Return False
            End If

            If s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then
                Return UInteger.TryParse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, result)
            End If

            Return UInteger.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, result)
        End Function

        ' ─────────────────────────── Переходы ───────────────────────────

        Private Function JumpTo(target As String) As Boolean
            target = target.Trim()

            Dim num As UInteger
            If TryParseNumber(target, num) Then
                _pc = CInt(num)
                Return True
            End If

            For i As Integer = 0 To _code.Count - 1
                If _code(i).Trim().Equals(target & ":", StringComparison.OrdinalIgnoreCase) Then
                    _pc = i
                    Return True
                End If
            Next

            Throw New FormatException($"Метка не найдена: {target}")
        End Function

        ' ─────────────────────────── Парсинг ───────────────────────────

        Private Shared Function StripComment(line As String) As String
            Dim idx = line.IndexOf(";"c)
            If idx >= 0 Then line = line.Substring(0, idx)
            idx = line.IndexOf("//", StringComparison.Ordinal)
            If idx >= 0 Then line = line.Substring(0, idx)
            Return line
        End Function

        Private Shared Sub ParseInstruction(line As String, ByRef mnemonic As String, ByRef operands As String())
            If line.Length = 0 Then
                mnemonic = "nop"
                operands = Array.Empty(Of String)()
                Return
            End If

            Dim spaceIdx = line.IndexOf(" "c)
            If spaceIdx < 0 Then
                mnemonic = line
                operands = Array.Empty(Of String)()
                Return
            End If

            mnemonic = line.Substring(0, spaceIdx).Trim()
            Dim rest = line.Substring(spaceIdx + 1).Trim()

            Dim list As New List(Of String)()
            Dim depth As Integer = 0
            Dim sb As New StringBuilder()
            For Each ch In rest
                Select Case ch
                    Case "["c
                        depth += 1 : sb.Append(ch)
                    Case "]"c
                        depth -= 1 : sb.Append(ch)
                    Case ","c
                        If depth = 0 Then
                            list.Add(sb.ToString().Trim())
                            sb.Clear()
                        Else
                            sb.Append(ch)
                        End If
                    Case Else
                        sb.Append(ch)
                End Select
            Next
            If sb.Length > 0 Then list.Add(sb.ToString().Trim())
            operands = list.ToArray()
        End Sub

    End Class

End Namespace