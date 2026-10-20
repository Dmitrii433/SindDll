' ─────────────────────────────────────────────────────────────────────────────
'  SindProgram.vb
'  Программа Sind: FileStruct + SindScript + ASM + ресурсы + компиляция + отладка.
' ─────────────────────────────────────────────────────────────────────────────

Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports SindDll.Scripts
Imports SindDll.Interpreter

''' <summary>Метаданные программы.</summary>
Public Class SindProgramInfo
    Public Property Name As String
    Public Property Version As String = "1.0"
    Public Property Author As String
    Public Property Description As String
    Public Property CreatedAt As DateTime = DateTime.Now
End Class

''' <summary>
''' Программа Sind: объединяет FileStruct, скрипт, ASM-код, ресурсы.
''' </summary>
Public Class SindProgram

    ' ─────────────────────────── Поля ───────────────────────────

    Private _file As FileStruct
    Private _script As SindScript
    Private _disposed As Boolean

    ' ─────────────────────────── Свойства ───────────────────────────

    ''' <summary>Метаданные программы.</summary>
    Public Property Info As New SindProgramInfo()

    ''' <summary>Описание файла (тип, точка входа, код, иконка, debug).</summary>
    Public Property File As FileStruct
        Get
            Return _file
        End Get
        Set(value As FileStruct)
            _file = value
        End Set
    End Property

    ''' <summary>Скриптовый движок.</summary>
    Public Property Script As SindScript
        Get
            If _script Is Nothing Then _script = New SindScript()
            Return _script
        End Get
        Set(value As SindScript)
            _script = value
        End Set
    End Property

    ''' <summary>Тип приложения.</summary>
    Public ReadOnly Property AppType As AppType
        Get
            Return _file.fi_apptype
        End Get
    End Property

    ''' <summary>Точка входа.</summary>
    Public ReadOnly Property EntryPoint As String
        Get
            Return If(_file.fi_entrypoint?.ToString(), String.Empty)
        End Get
    End Property

    ''' <summary>Включена ли отладка.</summary>
    Public ReadOnly Property DebugEnabled As Bool
        Get
            Return _file.fi_debugenabled
        End Get
    End Property

    ''' <summary>Количество строк кода.</summary>
    Public ReadOnly Property CodeLineCount As Integer
        Get
            Return If(_file.fi_code Is Nothing, 0, _file.fi_code.Length)
        End Get
    End Property

    ''' <summary>Количество ресурсов (иконка + пользовательские).</summary>
    Public ReadOnly Property ResourceCount As Integer
        Get
            Dim n As Integer = 0
            If _file.fi_icon IsNot Nothing Then n += 1
            If _resources IsNot Nothing Then n += _resources.Count
            Return n
        End Get
    End Property

    ' Пользовательские ресурсы
    Private ReadOnly _resources As New List(Of Resource)()

    ''' <summary>Пользовательские ресурсы.</summary>
    Public ReadOnly Property Resources As IReadOnlyList(Of Resource)
        Get
            Return _resources
        End Get
    End Property

    ' ─────────────────────────── Конструкторы ───────────────────────────

    Public Sub New()
        _file = New FileStruct()
        _file.fi_apptype = AppType.None
        _file.fi_entrypoint = New NativeString()
        _file.fi_code = New NativeArray(Of NativeString)(0)
        _file.fi_icon = Nothing
        _file.fi_debugenabled = Bool.False
        _script = New SindScript()
    End Sub

    Public Sub New(name As String, appType As AppType)
        Me.New()
        Info.Name = name
        _file.fi_apptype = appType
    End Sub

    ' ─────────────────────────── Код ───────────────────────────

    ''' <summary>Устанавливает точку входа.</summary>
    Public Sub SetEntryPoint(entry As String)
        If _file.fi_entrypoint IsNot Nothing Then _file.fi_entrypoint.Dispose()
        _file.fi_entrypoint = New NativeString(entry)
    End Sub

    ''' <summary>Заменяет код строками ASM.</summary>
    Public Sub SetCode(lines As IEnumerable(Of String))
        If _file.fi_code IsNot Nothing Then _file.fi_code.Dispose()
        Dim list = If(lines, Array.Empty(Of String)()).ToArray()
        Dim arr As New NativeArray(Of NativeString)(list.Length)
        For i As Integer = 0 To list.Length - 1
            arr(i) = New NativeString(list(i))
        Next
        _file.fi_code = arr
    End Sub

    ''' <summary>Добавляет строку ASM-кода.</summary>
    Public Sub AddCodeLine(line As String)
        Dim old = If(_file.fi_code, New NativeArray(Of NativeString)(0))
        Dim newArr As New NativeArray(Of NativeString)(old.Length + 1)
        For i As Integer = 0 To old.Length - 1
            newArr(i) = old(i)
        Next
        newArr(old.Length) = New NativeString(line)
        If _file.fi_code IsNot Nothing Then _file.fi_code.Dispose()
        _file.fi_code = newArr
    End Sub

    ''' <summary>Возвращает код как управляемый массив строк.</summary>
    Public Function GetCodeLines() As String()
        If _file.fi_code Is Nothing Then Return Array.Empty(Of String)()
        Dim result(_file.fi_code.Length - 1) As String
        For i As Integer = 0 To _file.fi_code.Length - 1
            result(i) = If(_file.fi_code(i)?.ToString(), String.Empty)
        Next
        Return result
    End Function

    ''' <summary>Очищает код.</summary>
    Public Sub ClearCode()
        If _file.fi_code IsNot Nothing Then _file.fi_code.Dispose()
        _file.fi_code = New NativeArray(Of NativeString)(0)
    End Sub

    ' ─────────────────────────── Ресурсы ───────────────────────────

    ''' <summary>Устанавливает иконку.</summary>
    Public Sub SetIcon(name As String, data As Byte())
        If _file.fi_icon IsNot Nothing Then _file.fi_icon.Dispose()
        _file.fi_icon = New Resource(name, "ICON", data)
    End Sub

    ''' <summary>Добавляет ресурс.</summary>
    Public Sub AddResource(res As Resource)
        If res Is Nothing Then Throw New ArgumentNullException(NameOf(res))
        _resources.Add(res)
    End Sub

    ''' <summary>Удаляет ресурс по имени.</summary>
    Public Function RemoveResource(name As String) As Boolean
        For i As Integer = 0 To _resources.Count - 1
            If String.Equals(_resources(i).Name?.ToString(), name, StringComparison.OrdinalIgnoreCase) Then
                _resources(i).Dispose()
                _resources.RemoveAt(i)
                Return True
            End If
        Next
        Return False
    End Function

    ''' <summary>Находит ресурс по имени.</summary>
    Public Function FindResource(name As String) As Resource
        For Each r In _resources
            If String.Equals(r.Name?.ToString(), name, StringComparison.OrdinalIgnoreCase) Then Return r
        Next
        Return Nothing
    End Function

    ''' <summary>Очищает пользовательские ресурсы.</summary>
    Public Sub ClearResources()
        For Each r In _resources
            r?.Dispose()
        Next
        _resources.Clear()
    End Sub

    ' ─────────────────────────── Скрипт ───────────────────────────

    ''' <summary>Загружает скрипт из текста.</summary>
    Public Sub LoadScriptText(text As String)
        Script.LoadScript(text)
    End Sub

    ''' <summary>Загружает скрипт из файла.</summary>
    Public Sub LoadScriptFile(path As String)
        Script.LoadScriptFile(path)
    End Sub

    ''' <summary>Выполняет загруженный скрипт.</summary>
    Public Function RunScript() As Boolean
        Return Script.Run()
    End Function

    ''' <summary>Выполняет одну строку скрипта.</summary>
    Public Function ExecuteScriptLine(line As String) As Boolean
        Return Script.ExecuteLine(line)
    End Function

    ' ─────────────────────────── Валидация ───────────────────────────

    ''' <summary>Проверяет корректность программы.</summary>
    Public Function Validate() As Boolean
        If String.IsNullOrWhiteSpace(Info.Name) Then Return False
        If _file.fi_apptype = AppType.None Then Return False
        If String.IsNullOrWhiteSpace(EntryPoint) Then Return False
        If CodeLineCount = 0 Then Return False
        Return True
    End Function

    ''' <summary>Возвращает список проблем (пусто — всё ок).</summary>
    Public Function GetValidationErrors() As List(Of String)
        Dim errors As New List(Of String)()
        If String.IsNullOrWhiteSpace(Info.Name) Then errors.Add("Имя программы не задано.")
        If _file.fi_apptype = AppType.None Then errors.Add("Тип приложения не задан.")
        If String.IsNullOrWhiteSpace(EntryPoint) Then errors.Add("Точка входа не задана.")
        If CodeLineCount = 0 Then errors.Add("Код пуст.")
        Return errors
    End Function

    ' ─────────────────────────── Компиляция ───────────────────────────

    ''' <summary>Компилирует программу в байты (через Compiler).</summary>
    Public Function Compile() As Byte()
        If Not Validate() Then
            Dim errs = GetValidationErrors()
            Throw New InvalidOperationException("Программа некорректна: " & String.Join("; ", errs))
        End If

        Using comp As New Compiler(_file)
            Return comp.Compile()
        End Using
    End Function

    ''' <summary>Компилирует в файл.</summary>
    Public Sub CompileToFile(path As String)
        Dim data = Compile()
        IO.File.WriteAllBytes(path, data)
    End Sub

    ' ─────────────────────────── Отладка ───────────────────────────

    ''' <summary>Возвращает ASM-интерпретатор с загруженным кодом.</summary>
    Public Function CreateInterpreter() As AsmInterpreter
        Dim interp As New AsmInterpreter()
        interp.Load(_file.fi_code)
        Return interp
    End Function

    ''' <summary>Возвращает отладчик (WinForms) с загруженной программой.</summary>
    Public Function CreateDebugger() As Object
        ' Возвращаем как Object, чтобы не тянуть ссылку на WinForms из библиотеки.
        ' В вызывающем коде: Dim dbg = CType(program.CreateDebugger(), SindDll.Debug.Debugger)
        Dim dbgType = Type.GetType("SindDll.Debug.Debugger, SindDll")
        If dbgType Is Nothing Then Return Nothing
        Dim dbg = Activator.CreateInstance(dbgType)
        Dim loadMethod = dbgType.GetMethod("LoadFile")
        loadMethod?.Invoke(dbg, New Object() {_file})
        Return dbg
    End Function

    ' ─────────────────────────── Сериализация ───────────────────────────

    ''' <summary>Сохраняет программу в текстовый формат (.sind).</summary>
    Public Sub SaveToFile(path As String)
        Dim sb As New StringBuilder()
        sb.AppendLine("[INFO]")
        sb.AppendLine("Name=" & Info.Name)
        sb.AppendLine("Version=" & Info.Version)
        sb.AppendLine("Author=" & Info.Author)
        sb.AppendLine("Description=" & Info.Description)
        sb.AppendLine("CreatedAt=" & Info.CreatedAt.ToString("O"))
        sb.AppendLine()
        sb.AppendLine("[FILE]")
        sb.AppendLine("AppType=" & _file.fi_apptype.ToString())
        sb.AppendLine("EntryPoint=" & EntryPoint)
        sb.AppendLine("DebugEnabled=" & If(_file.fi_debugenabled = Bool.True, "true", "false"))
        sb.AppendLine()
        sb.AppendLine("[CODE]")
        For Each line In GetCodeLines()
            sb.AppendLine(line)
        Next
        sb.AppendLine()
        sb.AppendLine("[RESOURCES]")
        If _file.fi_icon IsNot Nothing Then
            sb.AppendLine("Icon=" & _file.fi_icon.Name?.ToString())
        End If
        For Each r In _resources
            sb.AppendLine("Resource=" & r.Name?.ToString() & ";" & r.Kind & ";" & r.Size)
        Next

        IO.File.WriteAllText(path, sb.ToString(), Encoding.UTF8)
    End Sub

    ''' <summary>Загружает программу из текстового формата (.sind).</summary>
    Public Sub LoadFromFile(path As String)
        If Not IO.File.Exists(path) Then Throw New FileNotFoundException("Файл программы не найден.", path)
        Dim lines = IO.File.ReadAllLines(path, Encoding.UTF8)
        Dim section As String = ""
        Dim code As New List(Of String)()

        For Each raw In lines
            Dim line = raw.Trim()
            If line.Length = 0 Then Continue For
            If line.StartsWith("[") AndAlso line.EndsWith("]") Then
                section = line.Substring(1, line.Length - 2).ToUpperInvariant()
                Continue For
            End If

            Select Case section
                Case "INFO"
                    Dim eq = line.IndexOf("="c)
                    If eq > 0 Then
                        Dim key = line.Substring(0, eq).Trim()
                        Dim value = line.Substring(eq + 1).Trim()
                        Select Case key.ToLowerInvariant()
                            Case "name" : Info.Name = value
                            Case "version" : Info.Version = value
                            Case "author" : Info.Author = value
                            Case "description" : Info.Description = value
                            Case "createdat"
                                Dim dt As DateTime
                                If DateTime.TryParse(value, dt) Then Info.CreatedAt = dt
                        End Select
                    End If

                Case "FILE"
                    Dim eq = line.IndexOf("="c)
                    If eq > 0 Then
                        Dim key = line.Substring(0, eq).Trim()
                        Dim value = line.Substring(eq + 1).Trim()
                        Select Case key.ToLowerInvariant()
                            Case "apptype"
                                Dim at As AppType
                                If [Enum].TryParse(value, at) Then _file.fi_apptype = at
                            Case "entrypoint" : SetEntryPoint(value)
                            Case "debugenabled" : _file.fi_debugenabled = If(value.Equals("true", StringComparison.OrdinalIgnoreCase), Bool.True, Bool.False)
                        End Select
                    End If

                Case "CODE"
                    code.Add(raw)
            End Select
        Next

        SetCode(code)
    End Sub

    ' ─────────────────────────── Освобождение ───────────────────────────

    Public Sub Dispose()
        If _disposed Then Return

        If _file.fi_entrypoint IsNot Nothing Then
            _file.fi_entrypoint.Dispose()
            _file.fi_entrypoint = Nothing
        End If
        If _file.fi_code IsNot Nothing Then
            _file.fi_code.Dispose()
            _file.fi_code = Nothing
        End If
        If _file.fi_icon IsNot Nothing Then
            _file.fi_icon.Dispose()
            _file.fi_icon = Nothing
        End If

        For Each r In _resources
            r?.Dispose()
        Next
        _resources.Clear()

        _script?.Dispose()
        _script = Nothing

        _disposed = True
    End Sub

    Public Overrides Function ToString() As String
        Return $"SindProgram('{Info.Name}', AppType={AppType}, EntryPoint='{EntryPoint}', Lines={CodeLineCount}, Resources={ResourceCount}, Debug={DebugEnabled})"
    End Function

End Class