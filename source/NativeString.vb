Imports System.Runtime.InteropServices

''' <summary>
''' Обёртка над нативной (неуправляемой) строкой UTF-16 (wchar_t*).
''' Память выделяется через Marshal.StringToHGlobalUni и освобождается в Dispose.
''' </summary>
Public Class NativeString

    ' Указатель на нативный буфер (IntPtr.Zero — пустая строка)
    Private _ptr As IntPtr
    Private _disposed As Boolean

    ' ─────────────────────────── Конструкторы ───────────────────────────

    Public Sub New()
        _ptr = IntPtr.Zero
    End Sub

    Public Sub New(value As String)
        If String.IsNullOrEmpty(value) Then
            _ptr = IntPtr.Zero
        Else
            _ptr = Marshal.StringToHGlobalUni(value)
        End If
    End Sub

    Public Sub New(pointer As IntPtr)
        If pointer = IntPtr.Zero Then
            _ptr = IntPtr.Zero
        Else
            Dim s As String = Marshal.PtrToStringUni(pointer)
            _ptr = If(String.IsNullOrEmpty(s), IntPtr.Zero, Marshal.StringToHGlobalUni(s))
        End If
    End Sub

    ' ─────────────────────────── Свойства ───────────────────────────

    ''' <summary>Указатель на нативную память (IntPtr.Zero для пустой строки).</summary>
    Public ReadOnly Property Pointer As IntPtr
        Get
            ThrowIfDisposed()
            Return _ptr
        End Get
    End Property

    ''' <summary>Длина строки в символах.</summary>
    Public ReadOnly Property Length As Integer
        Get
            ThrowIfDisposed()
            Return If(_ptr = IntPtr.Zero, 0, ToManaged().Length)
        End Get
    End Property

    ''' <summary>Признак пустой строки.</summary>
    Public ReadOnly Property IsEmpty As Boolean
        Get
            ThrowIfDisposed()
            Return _ptr = IntPtr.Zero
        End Get
    End Property

    ' ─────────────────────────── Преобразования ───────────────────────────

    ''' <summary>Копирует нативную строку в управляемую .NET-строку.</summary>
    Public Function ToManaged() As String
        ThrowIfDisposed()
        If _ptr = IntPtr.Zero Then Return String.Empty
        Return Marshal.PtrToStringUni(_ptr)
    End Function

    Public Overrides Function ToString() As String
        Return ToManaged()
    End Function

    Public Shared Widening Operator CType(s As String) As NativeString
        Return New NativeString(s)
    End Operator

    Public Shared Narrowing Operator CType(ns As NativeString) As String
        If ns Is Nothing Then Return Nothing
        Return ns.ToManaged()
    End Operator

    ' ─────────────────────────── Операции ───────────────────────────

    ''' <summary>Возвращает новую NativeString — результат конкатенации.</summary>
    Public Function Concat(other As NativeString) As NativeString
        ThrowIfDisposed()
        If other Is Nothing Then Return New NativeString(ToManaged())
        Return New NativeString(ToManaged() & other.ToManaged())
    End Function

    Public Shared Operator +(a As NativeString, b As NativeString) As NativeString
        If a Is Nothing Then Return If(b Is Nothing, New NativeString(), New NativeString(b.ToManaged()))
        Return a.Concat(b)
    End Operator

    Public Shared Operator =(a As NativeString, b As NativeString) As Boolean
        If ReferenceEquals(a, b) Then Return True
        If a Is Nothing OrElse b Is Nothing Then Return False
        Return String.Equals(a.ToManaged(), b.ToManaged(), StringComparison.Ordinal)
    End Operator

    Public Shared Operator <>(a As NativeString, b As NativeString) As Boolean
        Return Not (a = b)
    End Operator

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other = TryCast(obj, NativeString)
        If other Is Nothing Then Return False
        Return Me = other
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return ToManaged().GetHashCode()
    End Function

    ''' <summary>Возвращает копию (новый блок нативной памяти).</summary>
    Public Function Clone() As NativeString
        ThrowIfDisposed()
        Return New NativeString(ToManaged())
    End Function

    ' ─────────────────────────── Освобождение ───────────────────────────

    ''' <summary>Освобождает нативную память.</summary>
    Public Sub Dispose()
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If _disposed Then Return
        If _ptr <> IntPtr.Zero Then
            Marshal.FreeHGlobal(_ptr)
            _ptr = IntPtr.Zero
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
        If _disposed Then Throw New ObjectDisposedException(NameOf(NativeString))
    End Sub

End Class