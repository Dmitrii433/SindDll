Imports System.Runtime.InteropServices

''' <summary>
''' Ресурс файла: имя, тип, данные в нативной памяти, размер и выравнивание.
''' </summary>
Public Class Resource

    ' ─────────────────────────── Поля ───────────────────────────

    Private _name As NativeString
    Private _data As NativeArray(Of Byte)
    Private _disposed As Boolean

    ' ─────────────────────────── Свойства ───────────────────────────

    ''' <summary>Имя ресурса (нативная строка).</summary>
    Public Property Name As NativeString
        Get
            ThrowIfDisposed()
            Return _name
        End Get
        Set(value As NativeString)
            ThrowIfDisposed()
            _name = value
        End Set
    End Property

    ''' <summary>Тип ресурса (например, "CODE", "DATA", "ICON").</summary>
    Public Property Kind As String

    ''' <summary>Данные ресурса (нативный массив байтов).</summary>
    Public Property Data As NativeArray(Of Byte)
        Get
            ThrowIfDisposed()
            Return _data
        End Get
        Set(value As NativeArray(Of Byte))
            ThrowIfDisposed()
            _data = value
        End Set
    End Property

    ''' <summary>Размер данных в байтах.</summary>
    Public ReadOnly Property Size As Integer
        Get
            ThrowIfDisposed()
            Return If(_data Is Nothing, 0, _data.Length)
        End Get
    End Property

    ''' <summary>Выравнивание ресурса в файле (1, 2, 4, 8, 16, 4096…).</summary>
    Public Property Alignment As Integer = 1

    ''' <summary>Смещение ресурса в файле (заполняется при сборке).</summary>
    Public Property Offset As Long

    ''' <summary>Сжатый ресурс (собственный Bool).</summary>
    Public Property Compressed As Bool = Bool.False

    ''' <summary>Зашифрованный ресурс (собственный Bool).</summary>
    Public Property Encrypted As Bool = Bool.False

    ''' <summary>Только для чтения (собственный Bool).</summary>
    Public Property ReadOnly_ As Bool = Bool.False

    ''' <summary>Признак пустого ресурса.</summary>
    Public ReadOnly Property IsEmpty As Bool
        Get
            ThrowIfDisposed()
            If _data Is Nothing OrElse _data.Length = 0 Then Return Bool.True
            Return Bool.False
        End Get
    End Property

    ' ─────────────────────────── Конструкторы ───────────────────────────

    Public Sub New()
        _name = New NativeString()
    End Sub

    Public Sub New(name As String, kind As String, data As Byte())
        If name Is Nothing Then Throw New ArgumentNullException(NameOf(name))
        If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))

        _name = New NativeString(name)
        Me.Kind = kind
        _data = New NativeArray(Of Byte)(data)
    End Sub

    Public Sub New(name As String, kind As String, size As Integer)
        If name Is Nothing Then Throw New ArgumentNullException(NameOf(name))
        If size < 0 Then Throw New ArgumentOutOfRangeException(NameOf(size))

        _name = New NativeString(name)
        Me.Kind = kind
        _data = New NativeArray(Of Byte)(size)
    End Sub

    ' ─────────────────────────── Данные ───────────────────────────

    ''' <summary>Возвращает копию данных в виде управляемого массива.</summary>
    Public Function GetBytes() As Byte()
        ThrowIfDisposed()
        If _data Is Nothing Then Return Array.Empty(Of Byte)()
        Return _data.ToManaged()
    End Function

    ''' <summary>Заменяет данные из управляемого массива.</summary>
    Public Sub SetBytes(data As Byte())
        ThrowIfDisposed()
        If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
        If _data IsNot Nothing Then _data.Dispose()
        _data = New NativeArray(Of Byte)(data)
    End Sub

    ''' <summary>Копирует часть данных в целевой массив.</summary>
    Public Sub Read(dest As Byte(), destIndex As Integer, sourceIndex As Integer, count As Integer)
        ThrowIfDisposed()
        If _data Is Nothing Then Throw New InvalidOperationException("Нет данных.")
        _data.CopyTo(dest, sourceIndex, destIndex, count)
    End Sub

    ''' <summary>Записывает часть данных из исходного массива.</summary>
    Public Sub Write(source As Byte(), sourceIndex As Integer, destIndex As Integer, count As Integer)
        ThrowIfDisposed()
        If _data Is Nothing Then Throw New InvalidOperationException("Нет данных.")
        _data.CopyFrom(source, sourceIndex, destIndex, count)
    End Sub

    ''' <summary>Указатель на данные (для P/Invoke). IntPtr.Zero если пусто.</summary>
    Public Function GetPointer() As IntPtr
        ThrowIfDisposed()
        Return If(_data Is Nothing, IntPtr.Zero, _data.Pointer)
    End Function

    ''' <summary>Возвращает копию ресурса (глубокая копия данных и имени).</summary>
    Public Function Clone() As Resource
        ThrowIfDisposed()
        Dim copy As New Resource()
        copy._name = New NativeString(If(_name?.ToString(), String.Empty))
        copy.Kind = Me.Kind
        copy.Alignment = Me.Alignment
        copy.Offset = Me.Offset
        copy.Compressed = Me.Compressed
        copy.Encrypted = Me.Encrypted
        copy.ReadOnly_ = Me.ReadOnly_
        copy._data = If(_data Is Nothing, Nothing, _data.Clone())
        Return copy
    End Function

    ' ─────────────────────────── Служебное ───────────────────────────

    Public Overrides Function ToString() As String
        ThrowIfDisposed()
        Return $"Resource[{Kind}] '{_name}' Size={Size} Align={Alignment} Offset=0x{Offset:X} " &
               $"Compressed={Compressed} Encrypted={Encrypted} ReadOnly={ReadOnly_}"
    End Function

    ''' <summary>Освобождает нативные ресурсы.</summary>
    Public Sub Dispose()
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If _disposed Then Return

        If _name IsNot Nothing Then
            _name.Dispose()
            _name = Nothing
        End If
        If _data IsNot Nothing Then
            _data.Dispose()
            _data = Nothing
        End If

        _disposed = True
    End Sub

    Protected Overrides Sub Finalize()
        Try
            Dispose(False)
        Finally
            MyBase.Finalize()
        End Try
    End Sub

    Private Sub ThrowIfDisposed()
        If _disposed Then Throw New ObjectDisposedException(NameOf(Resource))
    End Sub

End Class