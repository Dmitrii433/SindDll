Imports System.IO
Imports System.Text

''' <summary>
''' Компилятор: принимает FileStruct (тип приложения, точка входа, код, иконка, debug-флаг)
''' и собирает итоговый бинарный файл.
''' </summary>
Public Class Compiler
    Implements IDisposable

    ' ─────────────────────────── Поля ───────────────────────────

    Private _file As FileStruct
    Private _disposed As Boolean

    ' ─────────────────────────── Свойства ───────────────────────────

    ''' <summary>Компилируемое описание файла.</summary>
    Public Property File As FileStruct
        Get
            ThrowIfDisposed()
            Return _file
        End Get
        Set(value As FileStruct)
            ThrowIfDisposed()
            _file = value
        End Set
    End Property

    ''' <summary>Включён ли режим отладки (из FileStruct).</summary>
    Public ReadOnly Property DebugEnabled As Bool
        Get
            ThrowIfDisposed()
            Return _file.fi_debugenabled
        End Get
    End Property

    ''' <summary>Тип приложения (из FileStruct).</summary>
    Public ReadOnly Property AppType As AppType
        Get
            ThrowIfDisposed()
            Return _file.fi_apptype
        End Get
    End Property

    ''' <summary>Последняя ошибка компиляции (Nothing, если успешно).</summary>
    Public Property LastError As String

    ''' <summary>Лог компиляции.</summary>
    Public ReadOnly Property Log As New List(Of String)

    ' ─────────────────────────── Конструкторы ───────────────────────────

    Public Sub New()
        _file = New FileStruct()
    End Sub

    Public Sub New(file As FileStruct)
        _file = file
    End Sub

    ' ─────────────────────────── Компиляция ───────────────────────────

    ''' <summary>Проверяет FileStruct на корректность.</summary>
    Public Function Validate() As Boolean
        ThrowIfDisposed()
        Log.Clear()
        LastError = Nothing

        If _file.fi_entrypoint Is Nothing OrElse _file.fi_entrypoint.IsEmpty Then
            LastError = "Точка входа не задана."
            Log.Add("[ERROR] " & LastError)
            Return False
        End If

        If _file.fi_code Is Nothing Then
            LastError = "Массив кода не инициализирован."
            Log.Add("[ERROR] " & LastError)
            Return False
        End If

        If _file.fi_code.Length = 0 Then
            LastError = "Код пуст."
            Log.Add("[ERROR] " & LastError)
            Return False
        End If

        If _file.fi_icon IsNot Nothing AndAlso _file.fi_icon.IsEmpty = Bool.True Then
            Log.Add("[WARN] Иконка пуста.")
        End If

        If _file.fi_debugenabled = Bool.True Then
            Log.Add("[INFO] Режим отладки включён.")
        End If

        Log.Add("[INFO] Валидация пройдена.")
        Return True
    End Function

    ''' <summary>Компилирует FileStruct в массив байтов.</summary>
    Public Function Compile() As Byte()
        ThrowIfDisposed()

        If Not Validate() Then
            Throw New InvalidOperationException("Компиляция не удалась: " & LastError)
        End If

        Log.Add($"[INFO] Начало компиляции: AppType={AppType}")

        Dim output As New MemoryStream()
        Dim writer As New BinaryWriter(output, Encoding.UTF8)

        ' ── Заголовок ──
        ' Магия "SND1" (Sind 1)
        writer.Write(New Byte() {&H53, &H4E, &H44, &H31})

        ' Версия формата
        writer.Write(CUShort(1))

        ' Тип приложения
        writer.Write(CInt(_file.fi_apptype))

        ' Debug-флаг (Bool как 1 байт)
        writer.Write(CByte(_file.fi_debugenabled))

        ' ── Точка входа ──
        Dim entry As String = If(_file.fi_entrypoint?.ToString(), String.Empty)
        writer.Write(entry.Length)                    ' длина строки
        writer.Write(Encoding.UTF8.GetBytes(entry))   ' сама строка

        ' ── Код ──
        writer.Write(_file.fi_code.Length)            ' количество строк
        For i As Integer = 0 To _file.fi_code.Length - 1
            Dim line As String = If(_file.fi_code(i)?.ToString(), String.Empty)
            writer.Write(line.Length)
            writer.Write(Encoding.UTF8.GetBytes(line))
        Next

        ' ── Иконка ──
        If _file.fi_icon IsNot Nothing AndAlso _file.fi_icon.IsEmpty = Bool.False Then
            Dim iconBytes As Byte() = _file.fi_icon.GetBytes()
            writer.Write(iconBytes.Length)
            writer.Write(iconBytes)
        Else
            writer.Write(0)                            ' нет иконки
        End If

        writer.Flush()
        Dim result As Byte() = output.ToArray()

        Log.Add($"[INFO] Компиляция завершена: {result.Length} байт")
        Return result
    End Function

    ''' <summary>Компилирует FileStruct и сохраняет в файл.</summary>
    Public Sub CompileToFile(path As String)
        ThrowIfDisposed()
        If path Is Nothing Then Throw New ArgumentNullException(NameOf(path))

        Dim data As Byte() = Compile()
        IO.File.WriteAllBytes(path, data)
        Log.Add($"[INFO] Файл сохранён: {path} ({data.Length} байт)")
    End Sub

    ''' <summary>Возвращает лог компиляции одной строкой.</summary>
    Public Function GetLogText() As String
        Return String.Join(Environment.NewLine, Log)
    End Function

    ' ─────────────────────────── Освобождение ───────────────────────────

    ''' <summary>Освобождает все ресурсы, используемые классом Compiler.</summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If _disposed Then Return

        If disposing Then
            ' Здесь освобождаются управляемые ресурсы (если они появятся в будущем)
            ' Например, если бы Log или MemoryStream были долгоживущими полями класса.
        End If

        _disposed = True
    End Sub

    Private Sub ThrowIfDisposed()
        If _disposed Then Throw New ObjectDisposedException(NameOf(Compiler))
    End Sub

End Class