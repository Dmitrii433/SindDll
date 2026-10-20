' ─────────────────────────────────────────────────────────────────────────────
'  SindScript.vb
'  Namespace Scripts
'  Скриптовый движок Sind: команды, переменные, условия, циклы,
'  метки/goto, call/ret, арифметика, строки, массивы, файлы, математика.
' ─────────────────────────────────────────────────────────────────────────────

Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text

Namespace Scripts

    ''' <summary>Тип значения переменной.</summary>
    Public Enum ValueKind
        Null
        Number
        StrValue
        BoolValue
        ArrayValue
    End Enum

    ''' <summary>Значение переменной (динамическая типизация).</summary>
    Public Class ScriptValue
        Public Property Kind As ValueKind = ValueKind.Null
        Public Property Num As Double
        Public Property Str As String
        Public Property B As Boolean
        Public Property Arr As List(Of ScriptValue)

        Public Sub New()
        End Sub

        Public Shared Function FromNumber(n As Double) As ScriptValue
            Return New ScriptValue With {.Kind = ValueKind.Number, .Num = n}
        End Function

        Public Shared Function FromString(s As String) As ScriptValue
            Return New ScriptValue With {.Kind = ValueKind.StrValue, .Str = If(s, String.Empty)}
        End Function

        Public Shared Function FromBoolean(b As Boolean) As ScriptValue
            Return New ScriptValue With {.Kind = ValueKind.BoolValue, .B = b}
        End Function

        Public Shared Function FromArray(items As List(Of ScriptValue)) As ScriptValue
            Return New ScriptValue With {.Kind = ValueKind.ArrayValue, .Arr = If(items, New List(Of ScriptValue)())}
        End Function

        Public Shared Function NullValue() As ScriptValue
            Return New ScriptValue()
        End Function

        Public Function ToStringValue() As String
            Select Case Kind
                Case ValueKind.Number : Return Num.ToString(CultureInfo.InvariantCulture)
                Case ValueKind.StrValue : Return Str
                Case ValueKind.BoolValue : Return If(B, "true", "false")
                Case ValueKind.ArrayValue : Return $"[{String.Join(", ", Arr.Select(Function(x) x.ToStringValue()))}]"
                Case Else : Return "null"
            End Select
        End Function

        Public Function ToNumber() As Double
            Select Case Kind
                Case ValueKind.Number : Return Num
                Case ValueKind.StrValue
                    Dim d As Double
                    Double.TryParse(Str, NumberStyles.Any, CultureInfo.InvariantCulture, d)
                    Return d
                Case ValueKind.BoolValue : Return If(B, 1.0, 0.0)
                Case Else : Return 0.0
            End Select
        End Function

        Public Function ToBoolean() As Boolean
            Select Case Kind
                Case ValueKind.BoolValue : Return B
                Case ValueKind.Number : Return Num <> 0
                Case ValueKind.StrValue
                    Return Not String.IsNullOrEmpty(Str) AndAlso
                           Str <> "0" AndAlso
                           Not Str.Equals("false", StringComparison.OrdinalIgnoreCase)
                Case ValueKind.ArrayValue : Return Arr IsNot Nothing AndAlso Arr.Count > 0
                Case Else : Return False
            End Select
        End Function

        Public Overrides Function ToString() As String
            Return ToStringValue()
        End Function
    End Class

    ''' <summary>Состояние выполнения скрипта.</summary>
    Public Enum ScriptState
        Idle
        Running
        Paused
        Stopped
        Finished
    End Enum

    ''' <summary>Аргументы события лога.</summary>
    Public Class ScriptLogEventArgs
        Inherits EventArgs
        Public Property Message As String
        Public Sub New(message As String)
            Me.Message = message
        End Sub
    End Class

    ''' <summary>Аргументы события ошибки.</summary>
    Public Class ScriptErrorEventArgs
        Inherits EventArgs
        Public Property Message As String
        Public Property Line As String
        Public Sub New(message As String, line As String)
            Me.Message = message
            Me.Line = line
        End Sub
    End Class

    ''' <summary>Метка в скрипте.</summary>
    Public Class ScriptLabel
        Public Property Name As String
        Public Property Index As Integer
        Public Property Line As String
    End Class

    ''' <summary>
    ''' Скриптовый движок Sind: команды, переменные, условия, циклы,
    ''' метки/goto, call/ret, арифметика, строки, массивы, файлы, математика.
    ''' </summary>
    Public Class SindScript

        ' ─────────────────────────── Поля ───────────────────────────

        Private _state As ScriptState = ScriptState.Idle
        Private _disposed As Boolean = False

        Private ReadOnly _commands As New Dictionary(Of String, Func(Of String(), Boolean))(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _vars As New Dictionary(Of String, ScriptValue)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _constants As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _labels As New Dictionary(Of String, ScriptLabel)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _lines As New List(Of String)()
        Private _pc As Integer = 0

        Private ReadOnly _callStack As New Stack(Of Integer)()
        Private ReadOnly _argStack As New Stack(Of String())()

        Private ReadOnly _whileStack As New Stack(Of Integer)()
        Private ReadOnly _forStack As New Stack(Of ForContext)()
        Private ReadOnly _repeatStack As New Stack(Of Integer)()
        Private ReadOnly _ifStack As New Stack(Of IfContext)()
        Private ReadOnly _tryBlocks As New Stack(Of Integer)()

        Private ReadOnly _log As New StringBuilder()
        Private ReadOnly _errors As New List(Of String)()
        Private ReadOnly _properties As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

        Private Class ForContext
            Public Property VariableName As String
            Public Property Current As Double
            Public Property Limit As Double
            Public Property [Step] As Double
            Public Property BodyIndex As Integer
        End Class

        Private Class IfContext
            Public Property Taken As Boolean
            Public Property ElseIndex As Integer
            Public Property EndIndex As Integer
        End Class

        ' ─────────────────────────── События ───────────────────────────

        Public Event StateChanged(sender As Object, state As ScriptState)
        Public Event LogWritten(sender As Object, e As ScriptLogEventArgs)
        Public Event ErrorRaised(sender As Object, e As ScriptErrorEventArgs)
        Public Event LineExecuted(sender As Object, index As Integer, line As String)

        ' ─────────────────────────── Свойства ───────────────────────────

        Public ReadOnly Property State As ScriptState
            Get
                Return _state
            End Get
        End Property

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _state = ScriptState.Running
            End Get
        End Property

        Public ReadOnly Property IsFinished As Boolean
            Get
                Return _state = ScriptState.Finished
            End Get
        End Property

        Public ReadOnly Property HasErrors As Boolean
            Get
                Return _errors.Count > 0
            End Get
        End Property

        Public ReadOnly Property Errors As IReadOnlyList(Of String)
            Get
                Return _errors
            End Get
        End Property

        Public ReadOnly Property Log As String
            Get
                Return _log.ToString()
            End Get
        End Property

        Public ReadOnly Property CommandNames As IReadOnlyCollection(Of String)
            Get
                Return _commands.Keys
            End Get
        End Property

        Public ReadOnly Property VariableNames As IReadOnlyCollection(Of String)
            Get
                Return _vars.Keys
            End Get
        End Property

        Public ReadOnly Property LabelNames As IReadOnlyCollection(Of String)
            Get
                Return _labels.Keys
            End Get
        End Property

        Public ReadOnly Property ProgramCounter As Integer
            Get
                Return _pc
            End Get
        End Property

        Public ReadOnly Property LineCount As Integer
            Get
                Return _lines.Count
            End Get
        End Property

        Public ReadOnly Property CallDepth As Integer
            Get
                Return _callStack.Count
            End Get
        End Property

        Public ReadOnly Property Properties As IReadOnlyDictionary(Of String, String)
            Get
                Return _properties
            End Get
        End Property

        ' ─────────────────────────── Конструктор ───────────────────────────

        Public Sub New()
            RegisterBuiltins()
        End Sub

        ' ─────────────────────────── Управление состоянием ───────────────────────────

        Public Sub Begin()
            If _state = ScriptState.Running Then
                AddError("Скрипт уже запущен.")
                Return
            End If
            _state = ScriptState.Running
            _pc = 0
            _callStack.Clear()
            _argStack.Clear()
            _whileStack.Clear()
            _forStack.Clear()
            _repeatStack.Clear()
            _ifStack.Clear()
            _tryBlocks.Clear()
            _errors.Clear()
            RaiseEvent StateChanged(Me, _state)
            WriteLog("[BEGIN] Скрипт запущен.")
        End Sub

        Public Sub [End]()
            If _state <> ScriptState.Running AndAlso _state <> ScriptState.Paused Then
                AddError("Скрипт не был запущен.")
                Return
            End If
            _state = ScriptState.Finished
            _callStack.Clear()
            _argStack.Clear()
            _whileStack.Clear()
            _forStack.Clear()
            _repeatStack.Clear()
            _ifStack.Clear()
            _tryBlocks.Clear()
            RaiseEvent StateChanged(Me, _state)
            WriteLog("[END] Скрипт завершён.")
        End Sub

        Public Sub Pause()
            If _state = ScriptState.Running Then
                _state = ScriptState.Paused
                RaiseEvent StateChanged(Me, _state)
                WriteLog("[PAUSE]")
            End If
        End Sub

        Public Sub [Continue]()
            If _state = ScriptState.Paused Then
                _state = ScriptState.Running
                RaiseEvent StateChanged(Me, _state)
                WriteLog("[CONTINUE]")
            End If
        End Sub

        Public Sub [Stop]()
            _state = ScriptState.Stopped
            _pc = 0
            _callStack.Clear()
            _argStack.Clear()
            _whileStack.Clear()
            _forStack.Clear()
            _repeatStack.Clear()
            _ifStack.Clear()
            _tryBlocks.Clear()
            RaiseEvent StateChanged(Me, _state)
            WriteLog("[STOP]")
        End Sub

        Public Sub Reset()
            _state = ScriptState.Idle
            _pc = 0
            _lines.Clear()
            _labels.Clear()
            _vars.Clear()
            _constants.Clear()
            _callStack.Clear()
            _argStack.Clear()
            _whileStack.Clear()
            _forStack.Clear()
            _repeatStack.Clear()
            _ifStack.Clear()
            _tryBlocks.Clear()
            _errors.Clear()
            _log.Clear()
            _properties.Clear()
            RaiseEvent StateChanged(Me, _state)
        End Sub

        ' ─────────────────────────── Регистрация команд ───────────────────────────

        Public Sub RegisterCommand(name As String, handler As Func(Of String(), Boolean))
            If String.IsNullOrWhiteSpace(name) Then Throw New ArgumentException("Имя команды пустое.", NameOf(name))
            If handler Is Nothing Then Throw New ArgumentNullException(NameOf(handler))
            _commands(name) = handler
        End Sub

        Public Function UnregisterCommand(name As String) As Boolean
            Return _commands.Remove(name)
        End Function

        Public Function HasCommand(name As String) As Boolean
            Return _commands.ContainsKey(name)
        End Function

        ' ─────────────────────────── Загрузка программы ───────────────────────────

        Public Sub LoadScript(text As String)
            If text Is Nothing Then Throw New ArgumentNullException(NameOf(text))

            _lines.Clear()
            _labels.Clear()

            Dim rawLines = text.Split({ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.None)
            Dim idx As Integer = 0
            For Each raw In rawLines
                Dim line = StripComment(raw).Trim()
                If line.Length = 0 Then
                    _lines.Add(String.Empty)
                    idx += 1
                    Continue For
                End If

                If line.EndsWith(":") AndAlso line.IndexOf(" "c) < 0 Then
                    Dim labelName = line.Substring(0, line.Length - 1).Trim()
                    _labels(labelName) = New ScriptLabel With {.Name = labelName, .Index = idx, .Line = line}
                    _lines.Add(String.Empty)
                Else
                    _lines.Add(raw)
                End If
                idx += 1
            Next
        End Sub

        Public Sub LoadScript(lines As IEnumerable(Of String))
            If lines Is Nothing Then Throw New ArgumentNullException(NameOf(lines))
            LoadScript(String.Join(Environment.NewLine, lines))
        End Sub

        Public Sub LoadScriptFile(path As String)
            If path Is Nothing Then Throw New ArgumentNullException(NameOf(path))
            If Not File.Exists(path) Then Throw New FileNotFoundException("Файл скрипта не найден.", path)
            LoadScript(File.ReadAllText(path))
        End Sub

        ' ─────────────────────────── Выполнение ───────────────────────────

        Public Function Run() As Boolean
            If _lines.Count = 0 Then
                AddError("Программа не загружена. Вызовите LoadScript().")
                Return False
            End If

            Begin()
            Do While _state = ScriptState.Running AndAlso _pc < _lines.Count
                Dim ok = ExecuteCurrentLine()
                If Not ok Then Return False
                If _state <> ScriptState.Running Then Exit Do
            Loop

            If _state = ScriptState.Running Then [End]()
            Return Not HasErrors
        End Function

        Public Function StepOnce() As Boolean
            If _lines.Count = 0 Then
                AddError("Программа не загружена.")
                Return False
            End If
            If _state = ScriptState.Idle Then Begin()
            If _state <> ScriptState.Running Then Return False
            If _pc >= _lines.Count Then
                [End]()
                Return False
            End If
            Return ExecuteCurrentLine()
        End Function

        Public Function ExecuteLine(line As String) As Boolean
            If _state = ScriptState.Idle Then Begin()
            If _state <> ScriptState.Running Then
                AddError("Скрипт не запущен.")
                Return False
            End If

            line = StripComment(line).Trim()
            If line.Length = 0 Then Return True

            Try
                Return Dispatch(line)
            Catch ex As Exception
                HandleException(line, ex)
                Return False
            End Try
        End Function

        Public Function ExecuteLines(lines As IEnumerable(Of String)) As Boolean
            If _state = ScriptState.Idle Then Begin()
            For Each l In lines
                If Not ExecuteLine(l) Then Return False
            Next
            Return True
        End Function

        Public Function ExecuteScript(text As String) As Boolean
            LoadScript(text)
            Return Run()
        End Function

        Public Function Invoke(name As String, ParamArray args As String()) As Boolean
            If Not _commands.ContainsKey(name) Then
                AddError($"Неизвестная команда: {name}")
                Return False
            End If
            Return _commands(name)(args)
        End Function

        ' ─────────────────────────── Внутреннее выполнение ───────────────────────────

        Private Function ExecuteCurrentLine() As Boolean
            Dim raw = _lines(_pc)
            Dim line = StripComment(raw).Trim()
            RaiseEvent LineExecuted(Me, _pc, raw)

            If line.Length = 0 Then
                _pc += 1
                Return True
            End If

            Dim prevPc = _pc
            Try
                Dim ok = Dispatch(line)
                If _pc = prevPc Then _pc += 1
                Return ok
            Catch ex As Exception
                HandleException(line, ex)
                If _pc = prevPc Then _pc += 1
                Return False
            End Try
        End Function

        Private Function Dispatch(line As String) As Boolean
            If StartsWithWord(line, "let") Then Return DoLet(Rest(line, "let"))
            If StartsWithWord(line, "set") Then Return DoSet(Rest(line, "set"))
            If StartsWithWord(line, "const") Then Return DoConst(Rest(line, "const"))
            If StartsWithWord(line, "inc") Then Return DoInc(Rest(line, "inc"))
            If StartsWithWord(line, "dec") Then Return DoDec(Rest(line, "dec"))
            If StartsWithWord(line, "if") Then Return DoIf(Rest(line, "if"))
            If StartsWithWord(line, "elseif") Then Return DoElseIf(Rest(line, "elseif"))
            If StartsWithWord(line, "else") Then Return DoElse()
            If StartsWithWord(line, "endif") Then Return DoEndIf()
            If StartsWithWord(line, "while") Then Return DoWhile(Rest(line, "while"))
            If StartsWithWord(line, "endwhile") Then Return DoEndWhile()
            If StartsWithWord(line, "for") Then Return DoFor(Rest(line, "for"))
            If StartsWithWord(line, "endfor") Then Return DoEndFor()
            If StartsWithWord(line, "repeat") Then Return DoRepeat(Rest(line, "repeat"))
            If StartsWithWord(line, "endrepeat") Then Return DoEndRepeat()
            If StartsWithWord(line, "goto") Then Return DoGoto(Rest(line, "goto"))
            If StartsWithWord(line, "call") Then Return DoCall(Rest(line, "call"))
            If StartsWithWord(line, "ret") Then Return DoRet()
            If StartsWithWord(line, "arg") Then Return DoArg(Rest(line, "arg"))
            If StartsWithWord(line, "try") Then Return DoTry()
            If StartsWithWord(line, "catch") Then Return DoCatch(Rest(line, "catch"))
            If StartsWithWord(line, "endtry") Then Return DoEndTry()
            If StartsWithWord(line, "include") Then Return DoInclude(Rest(line, "include"))
            If StartsWithWord(line, "print") Then Return DoPrint(Rest(line, "print"))
            If StartsWithWord(line, "echo") Then Return DoPrint(Rest(line, "echo"))
            If StartsWithWord(line, "debug") Then Return DoDebug(Rest(line, "debug"))
            If StartsWithWord(line, "assert") Then Return DoAssert(Rest(line, "assert"))
            If StartsWithWord(line, "throw") Then Return DoThrow(Rest(line, "throw"))
            If StartsWithWord(line, "sleep") Then Return DoSleep(Rest(line, "sleep"))
            If StartsWithWord(line, "exit") Then [End]() : Return True
            If StartsWithWord(line, "halt") Then [End]() : Return True
            If StartsWithWord(line, "nop") Then Return True

            Dim parts = SplitCommand(line)
            If parts.Length = 0 Then Return True

            Dim cmd = parts(0)
            Dim args = If(parts.Length > 1, parts.Skip(1).ToArray(), Array.Empty(Of String)())

            If Not _commands.ContainsKey(cmd) Then
                If line.Contains("=") AndAlso Not line.Contains("==") Then
                    Return DoLet(line)
                End If
                AddError($"Неизвестная команда: {cmd}")
                Return False
            End If

            Dim ok = _commands(cmd)(args)
            If ok Then WriteLog($"[OK] {line}") Else WriteLog($"[FAIL] {line}")
            Return ok
        End Function

        ' ─────────────────────────── Переменные ───────────────────────────

        Public Sub SetVariable(name As String, value As ScriptValue)
            If String.IsNullOrWhiteSpace(name) Then Throw New ArgumentException("Имя пустое.", NameOf(name))
            If _constants.Contains(name) Then Throw New InvalidOperationException($"'{name}' — константа, нельзя изменить.")
            _vars(name) = value
        End Sub

        Public Sub SetVariable(name As String, value As String)
            SetVariable(name, ScriptValue.FromString(value))
        End Sub

        Public Sub SetNumber(name As String, value As Double)
            SetVariable(name, ScriptValue.FromNumber(value))
        End Sub

        Public Sub SetBoolean(name As String, value As Boolean)
            SetVariable(name, ScriptValue.FromBoolean(value))
        End Sub

        Public Function GetVariable(name As String) As ScriptValue
            Dim v As ScriptValue = Nothing
            If _vars.TryGetValue(name, v) Then Return v
            Return ScriptValue.NullValue()
        End Function

        Public Function GetVariableString(name As String) As String
            Return GetVariable(name).ToStringValue()
        End Function

        Public Function GetVariableNumber(name As String) As Double
            Return GetVariable(name).ToNumber()
        End Function

        Public Function RemoveVariable(name As String) As Boolean
            If _constants.Contains(name) Then Return False
            Return _vars.Remove(name)
        End Function

        Public Sub ClearVariables()
            _vars.Clear()
            _constants.Clear()
        End Sub

        Public Function HasVariable(name As String) As Boolean
            Return _vars.ContainsKey(name)
        End Function

        ' ─────────────────────────── Свойства окружения ───────────────────────────

        Public Sub SetProperty(name As String, value As String)
            _properties(name) = value
        End Sub

        Public Function GetProperty(name As String) As String
            Dim v As String = Nothing
            If _properties.TryGetValue(name, v) Then Return v
            Return Nothing
        End Function

        ' ─────────────────────────── Выражения ───────────────────────────

        Public Function Evaluate(expr As String) As ScriptValue
            If expr Is Nothing Then Return ScriptValue.NullValue()
            expr = expr.Trim()
            If expr.Length = 0 Then Return ScriptValue.NullValue()

            If (expr.StartsWith("""") AndAlso expr.EndsWith("""")) OrElse
               (expr.StartsWith("'") AndAlso expr.EndsWith("'")) Then
                Return ScriptValue.FromString(expr.Substring(1, expr.Length - 2))
            End If

            If expr.Equals("true", StringComparison.OrdinalIgnoreCase) Then Return ScriptValue.FromBoolean(True)
            If expr.Equals("false", StringComparison.OrdinalIgnoreCase) Then Return ScriptValue.FromBoolean(False)
            If expr.Equals("null", StringComparison.OrdinalIgnoreCase) Then Return ScriptValue.NullValue()

            Dim num As Double
            If Double.TryParse(expr, NumberStyles.Any, CultureInfo.InvariantCulture, num) Then
                Return ScriptValue.FromNumber(num)
            End If

            Dim expanded = ExpandVariables(expr)

            If Double.TryParse(expanded, NumberStyles.Any, CultureInfo.InvariantCulture, num) Then
                Return ScriptValue.FromNumber(num)
            End If

            If _vars.ContainsKey(expanded) Then Return _vars(expanded)

            Dim v = TryArithmetic(expanded)
            If v IsNot Nothing Then Return v

            Dim cmp As Integer
            If TryCompare(expanded, cmp) Then
                Return ScriptValue.FromNumber(cmp)
            End If

            If expanded.StartsWith("[") AndAlso expanded.EndsWith("]") Then
                Return ParseArray(expanded)
            End If

            Return ScriptValue.FromString(expanded)
        End Function

        Private Function TryArithmetic(expr As String) As ScriptValue
            Dim ops = {"+", "-", "*", "/", "%"}
            For Each op In ops
                Dim idx = FindTopLevelOperator(expr, op)
                If idx > 0 Then
                    Dim leftStr = expr.Substring(0, idx).Trim()
                    Dim rightStr = expr.Substring(idx + op.Length).Trim()
                    Dim left = Evaluate(leftStr)
                    Dim right = Evaluate(rightStr)

                    If op = "+" AndAlso (left.Kind = ValueKind.StrValue OrElse right.Kind = ValueKind.StrValue) Then
                        Return ScriptValue.FromString(left.ToStringValue() & right.ToStringValue())
                    End If

                    Dim a = left.ToNumber()
                    Dim b = right.ToNumber()

                    Select Case op
                        Case "+" : Return ScriptValue.FromNumber(a + b)
                        Case "-" : Return ScriptValue.FromNumber(a - b)
                        Case "*" : Return ScriptValue.FromNumber(a * b)
                        Case "/"
                            If b = 0 Then Throw New DivideByZeroException("Деление на ноль.")
                            Return ScriptValue.FromNumber(a / b)
                        Case "%"
                            If b = 0 Then Throw New DivideByZeroException("Деление на ноль.")
                            Return ScriptValue.FromNumber(a Mod b)
                    End Select
                End If
            Next
            Return Nothing
        End Function

        Private Function FindTopLevelOperator(expr As String, op As String) As Integer
            Dim depth As Integer = 0
            Dim inStr As Boolean = False
            Dim quoteCh As Char = """"c

            For i As Integer = expr.Length - op.Length To 0 Step -1
                Dim ch = expr(i)

                If ch = """"c OrElse ch = "'"c Then
                    If Not inStr Then
                        inStr = True : quoteCh = ch
                    ElseIf ch = quoteCh Then
                        inStr = False
                    End If
                End If

                If inStr Then Continue For

                If ch = ")"c Then depth += 1
                If ch = "("c Then depth -= 1

                If depth = 0 AndAlso i + op.Length <= expr.Length Then
                    If expr.Substring(i, op.Length) = op Then
                        If op = "+" AndAlso i > 0 Then
                            If "+-*/%(".Contains(expr(i - 1)) Then Continue For
                        End If
                        If op = "-" AndAlso i > 0 Then
                            If "+-*/%(".Contains(expr(i - 1)) Then Continue For
                        End If
                        Return i
                    End If
                End If
            Next
            Return -1
        End Function

        Private Function TryCompare(expr As String, ByRef cmp As Integer) As Boolean
            For Each op In {"==", "!=", ">=", "<=", ">", "<"}
                Dim idx = FindTopLevelOperator(expr, op)
                If idx > 0 Then
                    Dim left = Evaluate(expr.Substring(0, idx).Trim())
                    Dim right = Evaluate(expr.Substring(idx + op.Length).Trim())

                    Dim bothNum = left.Kind = ValueKind.Number AndAlso right.Kind = ValueKind.Number
                    Dim rawCmp As Integer
                    If bothNum Then
                        rawCmp = left.Num.CompareTo(right.Num)
                    Else
                        rawCmp = String.CompareOrdinal(left.ToStringValue(), right.ToStringValue())
                    End If

                    Select Case op
                        Case "==" : cmp = If(rawCmp = 0, 0, 1)
                        Case "!=" : cmp = If(rawCmp <> 0, 0, 1)
                        Case ">" : cmp = If(rawCmp > 0, 0, 1)
                        Case "<" : cmp = If(rawCmp < 0, 0, 1)
                        Case ">=" : cmp = If(rawCmp >= 0, 0, 1)
                        Case "<=" : cmp = If(rawCmp <= 0, 0, 1)
                    End Select
                    Return True
                End If
            Next
            Return False
        End Function

        Private Function ParseArray(expr As String) As ScriptValue
            Dim inner = expr.Substring(1, expr.Length - 2).Trim()
            Dim list As New List(Of ScriptValue)()
            If inner.Length > 0 Then
                Dim parts = SplitTopLevel(inner, ","c)
                For Each p In parts
                    list.Add(Evaluate(p.Trim()))
                Next
            End If
            Return ScriptValue.FromArray(list)
        End Function

        Private Function SplitTopLevel(expr As String, sep As Char) As List(Of String)
            Dim result As New List(Of String)()
            Dim depth As Integer = 0
            Dim inStr As Boolean = False
            Dim sb As New StringBuilder()

            For Each ch In expr
                If ch = """"c OrElse ch = "'"c Then inStr = Not inStr

                If Not inStr Then
                    If ch = "["c OrElse ch = "("c OrElse ch = "{"c Then depth += 1
                    If ch = "]"c OrElse ch = ")"c OrElse ch = "}"c Then depth -= 1

                    If depth = 0 AndAlso ch = sep Then
                        result.Add(sb.ToString())
                        sb.Clear()
                        Continue For
                    End If
                End If
                sb.Append(ch)
            Next
            If sb.Length > 0 Then result.Add(sb.ToString())
            Return result
        End Function

        Public Function ExpandVariables(text As String) As String
            If String.IsNullOrEmpty(text) Then Return text
            Dim result = text
            For Each kv In _vars
                result = result.Replace("%" & kv.Key & "%", kv.Value.ToStringValue())
                result = result.Replace("${" & kv.Key & "}", kv.Value.ToStringValue())
            Next
            For Each kv In _properties
                result = result.Replace("%" & kv.Key & "%", kv.Value)
                result = result.Replace("${" & kv.Key & "}", kv.Value)
            Next
            Return result
        End Function

        ' ─────────────────────────── Директивы: присваивание ───────────────────────────

        Private Function DoLet(rest As String) As Boolean
            Dim eq = rest.IndexOf("="c)
            If eq < 0 Then
                AddError("let требует 'name = value'")
                Return False
            End If
            Dim name = rest.Substring(0, eq).Trim()
            Dim value = Evaluate(rest.Substring(eq + 1).Trim())
            SetVariable(name, value)
            WriteLog($"[LET] {name} = {value}")
            Return True
        End Function

        Private Function DoSet(rest As String) As Boolean
            Dim eq = rest.IndexOf("="c)
            If eq < 0 Then
                AddError("set требует 'name = value'")
                Return False
            End If
            Dim name = rest.Substring(0, eq).Trim()
            Dim value = Evaluate(rest.Substring(eq + 1).Trim())
            SetVariable(name, value)
            WriteLog($"[SET] {name} = {value}")
            Return True
        End Function

        Private Function DoConst(rest As String) As Boolean
            Dim eq = rest.IndexOf("="c)
            If eq < 0 Then
                AddError("const требует 'name = value'")
                Return False
            End If
            Dim name = rest.Substring(0, eq).Trim()
            Dim value = Evaluate(rest.Substring(eq + 1).Trim())
            SetVariable(name, value)
            _constants.Add(name)
            WriteLog($"[CONST] {name} = {value}")
            Return True
        End Function

        Private Function DoInc(rest As String) As Boolean
            Dim name = rest.Trim()
            If name.Length = 0 Then
                AddError("inc требует имя переменной")
                Return False
            End If
            Dim v = GetVariable(name).ToNumber() + 1
            SetVariable(name, ScriptValue.FromNumber(v))
            Return True
        End Function

        Private Function DoDec(rest As String) As Boolean
            Dim name = rest.Trim()
            If name.Length = 0 Then
                AddError("dec требует имя переменной")
                Return False
            End If
            Dim v = GetVariable(name).ToNumber() - 1
            SetVariable(name, ScriptValue.FromNumber(v))
            Return True
        End Function

        ' ─────────────────────────── Директивы: if/else/endif ───────────────────────────

        Private Function DoIf(expr As String) As Boolean
            Dim cond = Evaluate(expr).ToBoolean()
            Dim ctx As New IfContext With {.Taken = cond, .ElseIndex = -1, .EndIndex = -1}
            _ifStack.Push(ctx)

            If Not cond Then
                Dim found = FindIfBranch(_pc + 1)
                If found >= 0 Then
                    _pc = found
                Else
                    AddError("if без endif")
                    Return False
                End If
            End If
            Return True
        End Function

        Private Function DoElseIf(expr As String) As Boolean
            If _ifStack.Count = 0 Then
                AddError("elseif без if")
                Return False
            End If
            Dim ctx = _ifStack.Peek()
            If ctx.Taken Then
                Dim endIdx = FindEndIf(_pc + 1)
                If endIdx < 0 Then
                    AddError("endif не найден")
                    Return False
                End If
                _pc = endIdx
                Return True
            End If

            Dim cond = Evaluate(expr).ToBoolean()
            If cond Then
                ctx.Taken = True
                Return True
            Else
                Dim found = FindIfBranch(_pc + 1)
                If found >= 0 Then
                    _pc = found
                Else
                    AddError("elseif без endif")
                    Return False
                End If
                Return True
            End If
        End Function

        Private Function DoElse() As Boolean
            If _ifStack.Count = 0 Then
                AddError("else без if")
                Return False
            End If
            Dim ctx = _ifStack.Peek()
            If ctx.Taken Then
                Dim endIdx = FindEndIf(_pc + 1)
                If endIdx < 0 Then
                    AddError("endif не найден")
                    Return False
                End If
                _pc = endIdx
                Return True
            End If
            ctx.Taken = True
            Return True
        End Function

        Private Function DoEndIf() As Boolean
            If _ifStack.Count > 0 Then _ifStack.Pop()
            Return True
        End Function

        Private Function FindIfBranch(startIdx As Integer) As Integer
            Dim depth As Integer = 0
            For i As Integer = startIdx To _lines.Count - 1
                Dim l = StripComment(_lines(i)).Trim()
                If StartsWithWord(l, "if") Then depth += 1
                If StartsWithWord(l, "endif") Then
                    If depth = 0 Then Return i
                    depth -= 1
                End If
                If depth = 0 AndAlso (StartsWithWord(l, "elseif") OrElse StartsWithWord(l, "else")) Then
                    Return i
                End If
            Next
            Return -1
        End Function

        Private Function FindEndIf(startIdx As Integer) As Integer
            Dim depth As Integer = 0
            For i As Integer = startIdx To _lines.Count - 1
                Dim l = StripComment(_lines(i)).Trim()
                If StartsWithWord(l, "if") Then depth += 1
                If StartsWithWord(l, "endif") Then
                    If depth = 0 Then Return i
                    depth -= 1
                End If
            Next
            Return -1
        End Function

        ' ─────────────────────────── Директивы: while ───────────────────────────

        Private Function DoWhile(expr As String) As Boolean
            Dim cond = Evaluate(expr).ToBoolean()
            If cond Then
                _whileStack.Push(_pc)
                Return True
            Else
                Dim endIdx = FindEndWhile(_pc + 1)
                If endIdx < 0 Then
                    AddError("endwhile не найден")
                    Return False
                End If
                _pc = endIdx
                Return True
            End If
        End Function

        Private Function DoEndWhile() As Boolean
            If _whileStack.Count = 0 Then
                AddError("endwhile без while")
                Return False
            End If
            Dim startIdx = _whileStack.Peek()
            Dim condLine = StripComment(_lines(startIdx)).Trim()
            Dim condExpr = Rest(condLine, "while")
            If Evaluate(condExpr).ToBoolean() Then
                _pc = startIdx
            Else
                _whileStack.Pop()
            End If
            Return True
        End Function

        Private Function FindEndWhile(startIdx As Integer) As Integer
            Dim depth As Integer = 0
            For i As Integer = startIdx To _lines.Count - 1
                Dim l = StripComment(_lines(i)).Trim()
                If StartsWithWord(l, "while") Then depth += 1
                If StartsWithWord(l, "endwhile") Then
                    If depth = 0 Then Return i
                    depth -= 1
                End If
            Next
            Return -1
        End Function

        ' ─────────────────────────── Директивы: for ───────────────────────────

        Private Function DoFor(rest As String) As Boolean
            Dim eqIdx = rest.IndexOf("="c)
            If eqIdx < 0 Then
                AddError("for требует 'for var = start to end [step N]'")
                Return False
            End If
            Dim varName = rest.Substring(0, eqIdx).Trim()
            Dim afterEq = rest.Substring(eqIdx + 1).Trim()

            Dim toIdx = afterEq.IndexOf(" to ", StringComparison.OrdinalIgnoreCase)
            If toIdx < 0 Then
                AddError("for требует 'to'")
                Return False
            End If
            Dim startExpr = afterEq.Substring(0, toIdx).Trim()
            Dim afterTo = afterEq.Substring(toIdx + 4).Trim()

            Dim stepVal As Double = 1
            Dim endExpr As String = afterTo
            Dim stepIdx = afterTo.IndexOf(" step ", StringComparison.OrdinalIgnoreCase)
            If stepIdx >= 0 Then
                endExpr = afterTo.Substring(0, stepIdx).Trim()
                stepVal = Evaluate(afterTo.Substring(stepIdx + 6).Trim()).ToNumber()
            End If

            Dim startNum = Evaluate(startExpr).ToNumber()
            Dim endNum = Evaluate(endExpr).ToNumber()

            Dim ctx As New ForContext With {
                .VariableName = varName,
                .Current = startNum,
                .Limit = endNum,
                .Step = stepVal,
                .BodyIndex = _pc + 1
            }
            _forStack.Push(ctx)
            SetVariable(varName, ScriptValue.FromNumber(startNum))

            Dim shouldRun As Boolean
            If stepVal >= 0 Then
                shouldRun = startNum <= endNum
            Else
                shouldRun = startNum >= endNum
            End If

            If Not shouldRun Then
                Dim endIdx = FindEndFor(_pc + 1)
                If endIdx < 0 Then
                    AddError("endfor не найден")
                    Return False
                End If
                _pc = endIdx
            End If
            Return True
        End Function

        Private Function DoEndFor() As Boolean
            If _forStack.Count = 0 Then
                AddError("endfor без for")
                Return False
            End If
            Dim ctx = _forStack.Peek()
            Dim nextVal = ctx.Current + ctx.Step

            Dim shouldContinue As Boolean
            If ctx.Step >= 0 Then
                shouldContinue = nextVal <= ctx.Limit
            Else
                shouldContinue = nextVal >= ctx.Limit
            End If

            If shouldContinue Then
                ctx.Current = nextVal
                SetVariable(ctx.VariableName, ScriptValue.FromNumber(nextVal))
                _pc = ctx.BodyIndex - 1
            Else
                _forStack.Pop()
            End If
            Return True
        End Function

        Private Function FindEndFor(startIdx As Integer) As Integer
            Dim depth As Integer = 0
            For i As Integer = startIdx To _lines.Count - 1
                Dim l = StripComment(_lines(i)).Trim()
                If StartsWithWord(l, "for") Then depth += 1
                If StartsWithWord(l, "endfor") Then
                    If depth = 0 Then Return i
                    depth -= 1
                End If
            Next
            Return -1
        End Function

        ' ─────────────────────────── Директивы: repeat ───────────────────────────

        Private Function DoRepeat(expr As String) As Boolean
            Dim count = CInt(Evaluate(expr).ToNumber())
            If count <= 0 Then
                Dim endIdx = FindEndRepeat(_pc + 1)
                If endIdx < 0 Then
                    AddError("endrepeat не найден")
                    Return False
                End If
                _pc = endIdx
                Return True
            End If
            _repeatStack.Push(_pc)
            Return True
        End Function

        Private Function DoEndRepeat() As Boolean
            If _repeatStack.Count = 0 Then
                AddError("endrepeat без repeat")
                Return False
            End If
            Dim startIdx = _repeatStack.Peek()
            Dim repeatLine = StripComment(_lines(startIdx)).Trim()
            Dim count = CInt(Evaluate(Rest(repeatLine, "repeat")).ToNumber())

            Dim counterVar = $"__repeat_{startIdx}"
            Dim cur = GetVariableNumber(counterVar) + 1
            SetVariable(counterVar, ScriptValue.FromNumber(cur))

            If cur < count Then
                _pc = startIdx
            Else
                _repeatStack.Pop()
                RemoveVariable(counterVar)
            End If
            Return True
        End Function

        Private Function FindEndRepeat(startIdx As Integer) As Integer
            Dim depth As Integer = 0
            For i As Integer = startIdx To _lines.Count - 1
                Dim l = StripComment(_lines(i)).Trim()
                If StartsWithWord(l, "repeat") Then depth += 1
                If StartsWithWord(l, "endrepeat") Then
                    If depth = 0 Then Return i
                    depth -= 1
                End If
            Next
            Return -1
        End Function

        ' ─────────────────────────── Директивы: goto/call/ret ───────────────────────────

        Private Function DoGoto(labelName As String) As Boolean
            labelName = labelName.Trim()
            If Not _labels.ContainsKey(labelName) Then
                AddError($"Метка не найдена: {labelName}")
                Return False
            End If
            _pc = _labels(labelName).Index
            Return True
        End Function

        Private Function DoCall(labelName As String) As Boolean
            labelName = labelName.Trim()
            If Not _labels.ContainsKey(labelName) Then
                AddError($"Метка не найдена: {labelName}")
                Return False
            End If
            _callStack.Push(_pc + 1)
            _pc = _labels(labelName).Index
            Return True
        End Function

        Private Function DoRet() As Boolean
            If _callStack.Count = 0 Then
                AddError("ret без call")
                Return False
            End If
            _pc = _callStack.Pop()
            Return True
        End Function

        Private Function DoArg(rest As String) As Boolean
            If _argStack.Count = 0 Then Return True
            Dim args = _argStack.Peek()
            Dim eqIdx = rest.IndexOf("="c)
            If eqIdx < 0 Then Return False
            Dim name = rest.Substring(0, eqIdx).Trim()
            Dim idx As Integer
            If Integer.TryParse(rest.Substring(eqIdx + 1).Trim(), idx) Then
                If idx >= 0 AndAlso idx < args.Length Then
                    SetVariable(name, ScriptValue.FromString(args(idx)))
                End If
            End If
            Return True
        End Function

        ' ─────────────────────────── Директивы: try/catch ───────────────────────────

        Private Function DoTry() As Boolean
            Dim catchIdx = FindCatch(_pc + 1)
            If catchIdx < 0 Then
                Dim endIdx = FindEndTry(_pc + 1)
                If endIdx < 0 Then
                    AddError("endtry не найден")
                    Return False
                End If
                _tryBlocks.Push(endIdx)
            Else
                _tryBlocks.Push(catchIdx)
            End If
            Return True
        End Function

        Private Function DoCatch(rest As String) As Boolean
            Dim endIdx = FindEndTry(_pc + 1)
            If endIdx >= 0 Then _pc = endIdx
            If _tryBlocks.Count > 0 Then _tryBlocks.Pop()
            Return True
        End Function

        Private Function DoEndTry() As Boolean
            If _tryBlocks.Count > 0 Then _tryBlocks.Pop()
            Return True
        End Function

        Private Function FindCatch(startIdx As Integer) As Integer
            Dim depth As Integer = 0
            For i As Integer = startIdx To _lines.Count - 1
                Dim l = StripComment(_lines(i)).Trim()
                If StartsWithWord(l, "try") Then depth += 1
                If StartsWithWord(l, "endtry") Then
                    If depth = 0 Then Return -1
                    depth -= 1
                End If
                If depth = 0 AndAlso StartsWithWord(l, "catch") Then Return i
            Next
            Return -1
        End Function

        Private Function FindEndTry(startIdx As Integer) As Integer
            Dim depth As Integer = 0
            For i As Integer = startIdx To _lines.Count - 1
                Dim l = StripComment(_lines(i)).Trim()
                If StartsWithWord(l, "try") Then depth += 1
                If StartsWithWord(l, "endtry") Then
                    If depth = 0 Then Return i
                    depth -= 1
                End If
            Next
            Return -1
        End Function

        ' ─────────────────────────── Директивы: include ───────────────────────────

        Private Function DoInclude(path As String) As Boolean
            path = path.Trim().Trim(""""c)
            If path.Length = 0 Then
                AddError("include требует путь")
                Return False
            End If
            If Not File.Exists(path) Then
                AddError($"Файл для include не найден: {path}")
                Return False
            End If
            Dim included = File.ReadAllText(path)
            Dim includedLines = included.Split({ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.None)

            Dim insertAt = _pc + 1
            For i As Integer = includedLines.Length - 1 To 0 Step -1
                _lines.Insert(insertAt, includedLines(i))
            Next
            WriteLog($"[INCLUDE] {path} ({includedLines.Length} строк)")
            Return True
        End Function

        ' ─────────────────────────── Директивы: вывод/диагностика ───────────────────────────

        Private Function DoPrint(rest As String) As Boolean
            Dim text = ExpandVariables(rest)
            Console.WriteLine(text)
            WriteLog("[PRINT] " & text)
            Return True
        End Function

        Private Function DoDebug(rest As String) As Boolean
            WriteLog("[DEBUG] " & ExpandVariables(rest))
            Return True
        End Function

        Private Function DoAssert(rest As String) As Boolean
            If Not Evaluate(rest).ToBoolean() Then
                Throw New InvalidOperationException("Assertion failed: " & rest)
            End If
            Return True
        End Function

        Private Function DoThrow(rest As String) As Boolean
            Throw New InvalidOperationException(ExpandVariables(rest))
        End Function

        Private Function DoSleep(rest As String) As Boolean
            Dim ms = CInt(Evaluate(rest).ToNumber())
            If ms > 0 Then Threading.Thread.Sleep(ms)
            Return True
        End Function

        ' ─────────────────────────── Встроенные команды ───────────────────────────

        Private Sub RegisterBuiltins()
            RegisterCommand("halt",
                Function(args As String()) As Boolean
                    [End]()
                    Return True
                End Function)

            RegisterCommand("exit",
                Function(args As String()) As Boolean
                    [End]()
                    Return True
                End Function)

            RegisterCommand("nop",
                Function(args As String()) As Boolean
                    Return True
                End Function)

            RegisterCommand("echo",
                Function(args As String()) As Boolean
                    Console.WriteLine(ExpandVariables(String.Join(" ", args)))
                    Return True
                End Function)

            RegisterCommand("set",
                Function(args As String()) As Boolean
                    If args.Length < 2 Then Return False
                    SetVariable(args(0), ExpandVariables(String.Join(" ", args.Skip(1))))
                    Return True
                End Function)

            RegisterCommand("inc",
                Function(args As String()) As Boolean
                    If args.Length <> 1 Then Return False
                    Dim v = GetVariableNumber(args(0)) + 1
                    SetVariable(args(0), ScriptValue.FromNumber(v))
                    Return True
                End Function)

            RegisterCommand("dec",
                Function(args As String()) As Boolean
                    If args.Length <> 1 Then Return False
                    Dim v = GetVariableNumber(args(0)) - 1
                    SetVariable(args(0), ScriptValue.FromNumber(v))
                    Return True
                End Function)

            RegisterCommand("error",
                Function(args As String()) As Boolean
                    AddError(ExpandVariables(String.Join(" ", args)))
                    Return False
                End Function)

            RegisterCommand("abs",
                Function(args As String()) As Boolean
                    If args.Length <> 1 Then Return False
                    Dim v = Evaluate(args(0)).ToNumber()
                    PushResult(Math.Abs(v))
                    Return True
                End Function)

            RegisterCommand("sqrt",
                Function(args As String()) As Boolean
                    If args.Length <> 1 Then Return False
                    PushResult(Math.Sqrt(Evaluate(args(0)).ToNumber()))
                    Return True
                End Function)

            RegisterCommand("pow",
                Function(args As String()) As Boolean
                    If args.Length <> 2 Then Return False
                    PushResult(Math.Pow(Evaluate(args(0)).ToNumber(), Evaluate(args(1)).ToNumber()))
                    Return True
                End Function)

            RegisterCommand("min",
                Function(args As String()) As Boolean
                    If args.Length < 1 Then Return False
                    Dim minVal = Double.MaxValue
                    For Each a In args
                        minVal = Math.Min(minVal, Evaluate(a).ToNumber())
                    Next
                    PushResult(minVal)
                    Return True
                End Function)

            RegisterCommand("max",
                Function(args As String()) As Boolean
                    If args.Length < 1 Then Return False
                    Dim maxVal = Double.MinValue
                    For Each a In args
                        maxVal = Math.Max(maxVal, Evaluate(a).ToNumber())
                    Next
                    PushResult(maxVal)
                    Return True
                End Function)

            RegisterCommand("len",
                Function(args As String()) As Boolean
                    If args.Length <> 1 Then Return False
                    Dim s = Evaluate(args(0)).ToStringValue()
                    PushResult(s.Length)
                    Return True
                End Function)

            RegisterCommand("upper",
                Function(args As String()) As Boolean
                    If args.Length <> 1 Then Return False
                    SetVariable("__result", ScriptValue.FromString(Evaluate(args(0)).ToStringValue().ToUpperInvariant()))
                    Return True
                End Function)

            RegisterCommand("lower",
                Function(args As String()) As Boolean
                    If args.Length <> 1 Then Return False
                    SetVariable("__result", ScriptValue.FromString(Evaluate(args(0)).ToStringValue().ToLowerInvariant()))
                    Return True
                End Function)

            RegisterCommand("trim",
                Function(args As String()) As Boolean
                    If args.Length <> 1 Then Return False
                    SetVariable("__result", ScriptValue.FromString(Evaluate(args(0)).ToStringValue().Trim()))
                    Return True
                End Function)

            RegisterCommand("substr",
                Function(args As String()) As Boolean
                    If args.Length < 2 Then Return False
                    Dim s = Evaluate(args(0)).ToStringValue()
                    Dim start As Integer = CInt(Evaluate(args(1)).ToNumber())
                    If start < 0 Then start = 0
                    If start >= s.Length Then
                        SetVariable("__result", ScriptValue.FromString(String.Empty))
                        Return True
                    End If
                    Dim len As Integer = If(args.Length >= 3, CInt(Evaluate(args(2)).ToNumber()), s.Length - start)
                    If start + len > s.Length Then len = s.Length - start
                    SetVariable("__result", ScriptValue.FromString(s.Substring(start, len)))
                    Return True
                End Function)

            RegisterCommand("concat",
                Function(args As String()) As Boolean
                    SetVariable("__result", ScriptValue.FromString(String.Join("", args.Select(Function(a) Evaluate(a).ToStringValue()))))
                    Return True
                End Function)

            RegisterCommand("readfile",
                Function(args As String()) As Boolean
                    If args.Length <> 1 Then Return False
                    Dim path = ExpandVariables(args(0)).Trim(""""c)
                    If Not File.Exists(path) Then
                        AddError("Файл не найден: " & path)
                        Return False
                    End If
                    SetVariable("__result", ScriptValue.FromString(File.ReadAllText(path)))
                    Return True
                End Function)

            RegisterCommand("writefile",
                Function(args As String()) As Boolean
                    If args.Length < 2 Then Return False
                    Dim path = ExpandVariables(args(0)).Trim(""""c)
                    Dim content = ExpandVariables(String.Join(" ", args.Skip(1))).Trim(""""c)
                    File.WriteAllText(path, content)
                    Return True
                End Function)

            RegisterCommand("appendfile",
                Function(args As String()) As Boolean
                    If args.Length < 2 Then Return False
                    Dim path = ExpandVariables(args(0)).Trim(""""c)
                    Dim content = ExpandVariables(String.Join(" ", args.Skip(1))).Trim(""""c)
                    File.AppendAllText(path, content)
                    Return True
                End Function)

            RegisterCommand("exists",
                Function(args As String()) As Boolean
                    If args.Length <> 1 Then Return False
                    Dim path = ExpandVariables(args(0)).Trim(""""c)
                    SetVariable("__result", ScriptValue.FromBoolean(File.Exists(path)))
                    Return True
                End Function)

            RegisterCommand("delete",
                Function(args As String()) As Boolean
                    If args.Length <> 1 Then Return False
                    Dim path = ExpandVariables(args(0)).Trim(""""c)
                    If File.Exists(path) Then File.Delete(path)
                    Return True
                End Function)

            RegisterCommand("array",
                Function(args As String()) As Boolean
                    Dim list As New List(Of ScriptValue)()
                    For Each a In args
                        list.Add(Evaluate(a))
                    Next
                    SetVariable("__result", ScriptValue.FromArray(list))
                    Return True
                End Function)

            RegisterCommand("push",
                Function(args As String()) As Boolean
                    If args.Length < 2 Then Return False
                    Dim arr = GetVariable(args(0))
                    If arr.Kind <> ValueKind.ArrayValue Then
                        arr = ScriptValue.FromArray(New List(Of ScriptValue)())
                        SetVariable(args(0), arr)
                    End If
                    arr.Arr.Add(Evaluate(args(1)))
                    Return True
                End Function)

            RegisterCommand("count",
                Function(args As String()) As Boolean
                    If args.Length <> 1 Then Return False
                    Dim arr = GetVariable(args(0))
                    If arr.Kind = ValueKind.ArrayValue Then
                        PushResult(arr.Arr.Count)
                    Else
                        PushResult(0)
                    End If
                    Return True
                End Function)

            RegisterCommand("random",
                Function(args As String()) As Boolean
                    Dim max As Integer = If(args.Length >= 1, CInt(Evaluate(args(0)).ToNumber()), 100)
                    PushResult(New Random().Next(max))
                    Return True
                End Function)

            RegisterCommand("time",
                Function(args As String()) As Boolean
                    SetVariable("__result", ScriptValue.FromString(DateTime.Now.ToString("HH:mm:ss")))
                    Return True
                End Function)

            RegisterCommand("date",
                Function(args As String()) As Boolean
                    SetVariable("__result", ScriptValue.FromString(DateTime.Now.ToString("yyyy-MM-dd")))
                    Return True
                End Function)
        End Sub

        Private Sub PushResult(value As Double)
            SetVariable("__result", ScriptValue.FromNumber(value))
        End Sub

        ' ─────────────────────────── Ошибки ───────────────────────────

        Private Sub AddError(message As String)
            _errors.Add(message)
            WriteLog("[ERROR] " & message)
            RaiseEvent ErrorRaised(Me, New ScriptErrorEventArgs(message, Nothing))
        End Sub

        Private Sub HandleException(line As String, ex As Exception)
            If _tryBlocks.Count > 0 Then
                Dim catchIdx = _tryBlocks.Pop()
                _pc = catchIdx
                WriteLog("[TRY] Переход на catch: " & catchIdx)
                Return
            End If
            AddError($"Ошибка выполнения '{line}': {ex.Message}")
            _state = ScriptState.Stopped
        End Sub

        Private Sub WriteLog(message As String)
            Dim line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}"
            _log.AppendLine(line)
            RaiseEvent LogWritten(Me, New ScriptLogEventArgs(line))
        End Sub

        ' ─────────────────────────── Утилиты ───────────────────────────

        Private Shared Function StripComment(line As String) As String
            Dim idx = line.IndexOf(";"c)
            If idx >= 0 Then line = line.Substring(0, idx)
            idx = line.IndexOf("//", StringComparison.Ordinal)
            If idx >= 0 Then line = line.Substring(0, idx)
            Return line
        End Function

        Private Shared Function SplitCommand(line As String) As String()
            Dim list As New List(Of String)()
            Dim sb As New StringBuilder()
            Dim inQuotes As Boolean = False

            For Each ch In line
                Select Case ch
                    Case """"c
                        inQuotes = Not inQuotes
                    Case " "c, ControlChars.Tab
                        If inQuotes Then
                            sb.Append(ch)
                        ElseIf sb.Length > 0 Then
                            list.Add(sb.ToString())
                            sb.Clear()
                        End If
                    Case Else
                        sb.Append(ch)
                End Select
            Next
            If sb.Length > 0 Then list.Add(sb.ToString())
            Return list.ToArray()
        End Function

        Private Shared Function StartsWithWord(line As String, word As String) As Boolean
            If Not line.StartsWith(word, StringComparison.OrdinalIgnoreCase) Then Return False
            If line.Length = word.Length Then Return True
            Dim nextCh = line(word.Length)
            Return nextCh = " "c OrElse nextCh = ControlChars.Tab
        End Function

        Private Shared Function Rest(line As String, word As String) As String
            If line.Length <= word.Length Then Return String.Empty
            Return line.Substring(word.Length).Trim()
        End Function

        ' ─────────────────────────── Освобождение ───────────────────────────

        Public Sub Dispose()
            If _disposed Then Return
            _commands.Clear()
            _vars.Clear()
            _constants.Clear()
            _labels.Clear()
            _lines.Clear()
            _callStack.Clear()
            _argStack.Clear()
            _whileStack.Clear()
            _forStack.Clear()
            _repeatStack.Clear()
            _ifStack.Clear()
            _tryBlocks.Clear()
            _errors.Clear()
            _log.Clear()
            _properties.Clear()
            _disposed = True
        End Sub

        Public Overrides Function ToString() As String
            Return $"SindScript(State={_state}, PC={_pc}, Lines={_lines.Count}, Commands={_commands.Count}, Vars={_vars.Count})"
        End Function

    End Class

End Namespace