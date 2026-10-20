Imports System.Runtime.InteropServices

''' <summary>
''' Массив элементов типа T.
''' • Для value-типов (blittable) данные лежат в неуправляемой памяти (AllocHGlobal).
''' • Для ссылочных типов (классы) используется управляемое хранилище.
''' </summary>
Public Class NativeArray(Of T)

    ' ─────────────────────────── Поля ───────────────────────────

    Private _ptr As IntPtr
    Private _managed As T()
    Private _length As Integer
    Private _disposed As Boolean

    Private Shared ReadOnly _isValueType As Boolean = GetType(T).IsValueType
    Private Shared ReadOnly _elemSize As Integer = CalcElementSize()

    ' ─────────────────────────── Конструкторы ───────────────────────────

    Public Sub New()
        _ptr = IntPtr.Zero
        _managed = Nothing
        _length = 0
    End Sub

    Public Sub New(length As Integer)
        If length < 0 Then Throw New ArgumentOutOfRangeException(NameOf(length))
        _length = length

        If _isValueType Then
            If length = 0 Then
                _ptr = IntPtr.Zero
            Else
                _ptr = Marshal.AllocHGlobal(length * _elemSize)
            End If
        Else
            _managed = If(length = 0, Array.Empty(Of T)(), New T(length - 1) {})
        End If
    End Sub

    Public Sub New(source As T())
        If source Is Nothing Then Throw New ArgumentNullException(NameOf(source))
        _length = source.Length

        If _isValueType Then
            If _length = 0 Then
                _ptr = IntPtr.Zero
            Else
                _ptr = Marshal.AllocHGlobal(_length * _elemSize)
                CopyFrom(source, 0, 0, _length)
            End If
        Else
            _managed = CType(source.Clone(), T())
        End If
    End Sub

    ' ─────────────────────────── Свойства ───────────────────────────

    Public ReadOnly Property Pointer As IntPtr
        Get
            ThrowIfDisposed()
            Return _ptr
        End Get
    End Property

    Public ReadOnly Property Length As Integer
        Get
            ThrowIfDisposed()
            Return _length
        End Get
    End Property

    Public ReadOnly Property ElementSize As Integer
        Get
            Return _elemSize
        End Get
    End Property

    Public ReadOnly Property ByteSize As Long
        Get
            ThrowIfDisposed()
            Return CLng(_length) * _elemSize
        End Get
    End Property

    Public ReadOnly Property IsEmpty As Boolean
        Get
            ThrowIfDisposed()
            Return _length = 0
        End Get
    End Property

    Public ReadOnly Property IsNative As Boolean
        Get
            Return _isValueType
        End Get
    End Property

    Default Public Property Item(index As Integer) As T
        Get
            ThrowIfDisposed()
            CheckIndex(index)
            If _isValueType Then
                Return Marshal.PtrToStructure(Of T)(_ptr + index * _elemSize)
            Else
                Return _managed(index)
            End If
        End Get
        Set(value As T)
            ThrowIfDisposed()
            CheckIndex(index)
            If _isValueType Then
                Marshal.StructureToPtr(value, _ptr + index * _elemSize, False)
            Else
                _managed(index) = value
            End If
        End Set
    End Property

    ' ─────────────────────────── Копирование ───────────────────────────

    ''' <summary>Копирует содержимое в новый управляемый массив.</summary>
    Public Function ToManaged() As T()
        ThrowIfDisposed()
        Dim result(_length - 1) As T
        If _length > 0 Then CopyTo(result, 0, 0, _length)
        Return result
    End Function

    ''' <summary>Копирует из управляемого массива в нативную память / в хранилище.</summary>
    Public Sub CopyFrom(source As T(), sourceIndex As Integer, destIndex As Integer, count As Integer)
        ThrowIfDisposed()
        If source Is Nothing Then Throw New ArgumentNullException(NameOf(source))
        CheckRange(sourceIndex, count, source.Length, NameOf(sourceIndex))
        CheckRange(destIndex, count, _length, NameOf(destIndex))
        If count = 0 Then Return

        If _isValueType Then
            ' Универсально для любого blittable T: пишем побайтово через Marshal.WriteByte
            For i As Integer = 0 To count - 1
                Dim value As T = source(sourceIndex + i)
                Dim dst = _ptr + (destIndex + i) * _elemSize
                WriteElement(dst, value)
            Next
        Else
            Array.Copy(source, sourceIndex, _managed, destIndex, count)
        End If
    End Sub

    ''' <summary>Копирует из нативной памяти / хранилища в управляемый массив.</summary>
    Public Sub CopyTo(dest As T(), sourceIndex As Integer, destIndex As Integer, count As Integer)
        ThrowIfDisposed()
        If dest Is Nothing Then Throw New ArgumentNullException(NameOf(dest))
        CheckRange(sourceIndex, count, _length, NameOf(sourceIndex))
        CheckRange(destIndex, count, dest.Length, NameOf(destIndex))
        If count = 0 Then Return

        If _isValueType Then
            For i As Integer = 0 To count - 1
                Dim src = _ptr + (sourceIndex + i) * _elemSize
                dest(destIndex + i) = ReadElement(src)
            Next
        Else
            Array.Copy(_managed, sourceIndex, dest, destIndex, count)
        End If
    End Sub

    ''' <summary>Копирует из другой NativeArray.</summary>
    Public Sub CopyFrom(source As NativeArray(Of T), sourceIndex As Integer, destIndex As Integer, count As Integer)
        ThrowIfDisposed()
        If source Is Nothing Then Throw New ArgumentNullException(NameOf(source))
        source.ThrowIfDisposed()
        CheckRange(sourceIndex, count, source._length, NameOf(sourceIndex))
        CheckRange(destIndex, count, _length, NameOf(destIndex))
        If count = 0 Then Return

        If _isValueType Then
            ' Побайтовое копирование между двумя нативными блоками — без указателей
            Dim srcBytes = count * _elemSize
            For i As Integer = 0 To srcBytes - 1
                Dim b As Byte = Marshal.ReadByte(source._ptr, sourceIndex * _elemSize + i)
                Marshal.WriteByte(_ptr, destIndex * _elemSize + i, b)
            Next
        Else
            Array.Copy(source._managed, sourceIndex, _managed, destIndex, count)
        End If
    End Sub

    ''' <summary>Возвращает копию части массива.</summary>
    Public Function Slice(startIndex As Integer, count As Integer) As NativeArray(Of T)
        ThrowIfDisposed()
        CheckRange(startIndex, count, _length, NameOf(startIndex))
        Dim result As New NativeArray(Of T)(count)
        If count > 0 Then result.CopyFrom(Me, startIndex, 0, count)
        Return result
    End Function

    ''' <summary>Заполняет массив значением.</summary>
    Public Sub Fill(value As T)
        ThrowIfDisposed()
        For i As Integer = 0 To _length - 1
            Me(i) = value
        Next
    End Sub

    ''' <summary>Очищает массив.</summary>
    Public Sub Clear()
        ThrowIfDisposed()
        If _isValueType Then
            If _ptr <> IntPtr.Zero Then
                For i As Integer = 0 To _length * _elemSize - 1
                    Marshal.WriteByte(_ptr, i, 0)
                Next
            End If
        Else
            Array.Clear(_managed, 0, _length)
        End If
    End Sub

    ''' <summary>Возвращает глубокую копию массива.</summary>
    Public Function Clone() As NativeArray(Of T)
        ThrowIfDisposed()
        Dim result As New NativeArray(Of T)(_length)
        If _length > 0 Then result.CopyFrom(Me, 0, 0, _length)
        Return result
    End Function

    ''' <summary>Ищет индекс элемента.</summary>
    Public Function IndexOf(value As T) As Integer
        ThrowIfDisposed()
        Dim cmp = Collections.Generic.EqualityComparer(Of T).Default
        For i As Integer = 0 To _length - 1
            If cmp.Equals(Me(i), value) Then Return i
        Next
        Return -1
    End Function

    ' ─────────────────────────── Освобождение ───────────────────────────

    Public Sub Dispose()
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If _disposed Then Return

        If _isValueType Then
            If _ptr <> IntPtr.Zero Then
                Marshal.FreeHGlobal(_ptr)
                _ptr = IntPtr.Zero
            End If
        Else
            If _managed IsNot Nothing Then
                For i As Integer = 0 To _managed.Length - 1
                    Dim item As Object = _managed(i)
                    If TypeOf item Is IDisposable Then
                        CType(item, IDisposable).Dispose()
                    End If
                    _managed(i) = Nothing
                Next
                _managed = Nothing
            End If
        End If

        _length = 0
        _disposed = True
    End Sub

    Protected Overrides Sub Finalize()
        Try
            Dispose(False)
        Finally
            MyBase.Finalize()
        End Try
    End Sub

    ' ─────────────────────────── Внутреннее ───────────────────────────

    Private Sub ThrowIfDisposed()
        If _disposed Then Throw New ObjectDisposedException(NameOf(NativeArray(Of T)))
    End Sub

    Private Sub CheckIndex(index As Integer)
        If index < 0 OrElse index >= _length Then
            Throw New IndexOutOfRangeException($"Индекс {index} вне [0..{_length - 1}]")
        End If
    End Sub

    Private Shared Sub CheckRange(startIndex As Integer, count As Integer, total As Integer, paramName As String)
        If startIndex < 0 OrElse count < 0 OrElse startIndex + count > total Then
            Throw New ArgumentOutOfRangeException(paramName,
                $"Диапазон [{startIndex}..{startIndex + count - 1}] вне [0..{total - 1}]")
        End If
    End Sub

    Private Shared Function CalcElementSize() As Integer
        If Not _isValueType Then Return IntPtr.Size
        Try
            Return Marshal.SizeOf(Of T)()
        Catch ex As Exception
            Throw New NotSupportedException(
                $"Тип {GetType(T).FullName} не поддерживается NativeArray(Of T).", ex)
        End Try
    End Function

    ''' <summary>Записывает значение T по указанному адресу без указателей.</summary>
    Private Shared Sub WriteElement(addr As IntPtr, value As T)
        ' Используем StructureToPtr (работает для любых blittable-структур).
        ' Для примитивов это тоже корректно.
        Marshal.StructureToPtr(value, addr, False)
    End Sub

    ''' <summary>Читает значение T по указанному адресу без указателей.</summary>
    Private Shared Function ReadElement(addr As IntPtr) As T
        Return Marshal.PtrToStructure(Of T)(addr)
    End Function

End Class