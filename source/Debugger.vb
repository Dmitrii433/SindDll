' ─────────────────────────────────────────────────────────────────────────────
'  Debugger.vb
'  Namespace Debug — логика формы отладчика (с ASM-интерпретатором).
' ─────────────────────────────────────────────────────────────────────────────

Imports System.Collections.Generic
Imports System.Text
Imports System.Windows.Forms
Imports System.Drawing

' Подключаем интерпретатор
Imports SindDll.Interpreter

Namespace Debug

    Partial Public Class Debugger

        ' ─────────────────────────── Состояние ───────────────────────────

        Private _file As FileStruct
        Private _state As DebuggerState = DebuggerState.Idle
        Private _currentLine As Integer = -1
        Private _breakpoints As New List(Of Integer)()
        Private _log As New StringBuilder()
        Private _watches As New Dictionary(Of String, String)()

        ' ASM-интерпретатор
        Private ReadOnly _interp As New AsmInterpreter()

        ' ─────────────────────────── Свойства ───────────────────────────

        Public ReadOnly Property State As DebuggerState
            Get
                Return _state
            End Get
        End Property

        Public ReadOnly Property CurrentLine As Integer
            Get
                Return _currentLine
            End Get
        End Property

        Public ReadOnly Property Cpu As AsmCpu
            Get
                Return _interp.CPU
            End Get
        End Property

        Public ReadOnly Property Interpreter As AsmInterpreter
            Get
                Return _interp
            End Get
        End Property

        ' ─────────────────────────── Загрузка ───────────────────────────

        Public Sub LoadFile(file As FileStruct)
            _file = file
            _currentLine = -1
            _breakpoints.Clear()
            _watches.Clear()
            _interp.Load(file.fi_code)
            SetState(DebuggerState.Idle)
            SyncCpuWatches()
            RefreshCodeList()
            RefreshWatches()
            RefreshStatus()
            WriteLog("[INFO] FileStruct загружен в ASM-интерпретатор.")
        End Sub

        ' ─────────────────────────── Отрисовка ───────────────────────────

        Private Sub RefreshCodeList()
            LstCode.BeginUpdate()
            Try
                LstCode.Items.Clear()
                If _file.fi_code Is Nothing Then Return

                For i As Integer = 0 To _file.fi_code.Length - 1
                    Dim text As String = If(_file.fi_code(i)?.ToString(), String.Empty)
                    Dim prefix As String = "   "
                    If _breakpoints.Contains(i) Then prefix = "●  "
                    If i = _currentLine Then prefix = "▶  "
                    If _breakpoints.Contains(i) AndAlso i = _currentLine Then prefix = "▶● "
                    LstCode.Items.Add($"{prefix}{i,3}: {text}")
                Next

                If _currentLine >= 0 AndAlso _currentLine < LstCode.Items.Count Then
                    LstCode.SelectedIndex = _currentLine
                End If
            Finally
                LstCode.EndUpdate()
            End Try
        End Sub

        Private Sub RefreshWatches()
            LstWatches.BeginUpdate()
            Try
                LstWatches.Items.Clear()
                For Each kv In _watches
                    Dim item As New ListViewItem(kv.Key)
                    item.SubItems.Add(kv.Value)
                    LstWatches.Items.Add(item)
                Next
            Finally
                LstWatches.EndUpdate()
            End Try
        End Sub

        Private Sub RefreshStatus()
            LblState.Text = $"Состояние: {_state}"
            LblLine.Text = $"Строка: {If(_currentLine >= 0, _currentLine.ToString(), "-")}"
            LblBp.Text = $"BP: {_breakpoints.Count}"
        End Sub

        Private Sub WriteLog(message As String)
            Dim line As String = $"[{DateTime.Now:HH:mm:ss.fff}] {message}"
            _log.AppendLine(line)
            TxtLog.AppendText(line & Environment.NewLine)
        End Sub

        Private Sub SyncCpuWatches()
            For Each kv In _interp.CPU.Snapshot()
                _watches(kv.Key) = kv.Value
            Next
        End Sub

        ' ─────────────────────────── Управление отладкой ───────────────────────────

        Private Sub StartDebug()
            If _file.fi_code Is Nothing OrElse _file.fi_code.Length = 0 Then
                MessageBox.Show("Нет кода для отладки.", "Debugger", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            _interp.Reset()
            _currentLine = 0
            SetState(DebuggerState.Running)
            SyncCpuWatches()
            RefreshCodeList()
            RefreshWatches()
            RefreshStatus()
            WriteLog("[INFO] Отладка запущена (ASM).")
        End Sub

        Private Sub StepOver()
            If _file.fi_code Is Nothing OrElse _file.fi_code.Length = 0 Then Return

            Dim res = _interp.StepOnce()

            If Not res.Ok Then
                WriteLog($"[ERROR] {res.ErrorMessage}")
                SetState(DebuggerState.Stopped)
                RefreshStatus()
                MessageBox.Show(res.ErrorMessage, "Ошибка интерпретатора",
                                MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            _currentLine = _interp.ProgramCounter
            If _currentLine >= _file.fi_code.Length Then _currentLine = _file.fi_code.Length - 1
            WriteLog($"[STEP] {_currentLine}: {res.Instruction}")

            SyncCpuWatches()

            If _breakpoints.Contains(_currentLine) AndAlso _currentLine > 0 Then
                SetState(DebuggerState.Paused)
                WriteLog($"[BP] Точка останова @{_currentLine}")
                MessageBox.Show($"Точка останова на строке {_currentLine}.", "Breakpoint",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

            If _interp.Halted Then
                _currentLine = _file.fi_code.Length - 1
                SetState(DebuggerState.Finished)
                WriteLog("[INFO] Выполнение завершено.")
            End If

            RefreshCodeList()
            RefreshWatches()
            RefreshStatus()
        End Sub

        Private Sub RunToEnd()
            If _state = DebuggerState.Idle OrElse _state = DebuggerState.Stopped Then StartDebug()

            Dim res = _interp.RunUntil(_breakpoints)

            If Not res.Ok Then
                WriteLog($"[ERROR] {res.ErrorMessage}")
                SetState(DebuggerState.Stopped)
                MessageBox.Show(res.ErrorMessage, "Ошибка интерпретатора",
                                MessageBoxButtons.OK, MessageBoxIcon.Error)
            ElseIf res.Halted Then
                _currentLine = _file.fi_code.Length - 1
                SetState(DebuggerState.Finished)
                WriteLog("[INFO] Выполнение завершено.")
            Else
                _currentLine = _interp.ProgramCounter
                SetState(DebuggerState.Paused)
                WriteLog($"[BP] Точка останова @{_currentLine}")
            End If

            SyncCpuWatches()
            RefreshCodeList()
            RefreshWatches()
            RefreshStatus()
        End Sub

        Private Sub PauseDebug()
            If _state = DebuggerState.Running Then
                SetState(DebuggerState.Paused)
                WriteLog("[INFO] Пауза.")
                RefreshStatus()
            End If
        End Sub

        Private Sub StopDebug()
            _currentLine = -1
            SetState(DebuggerState.Stopped)
            WriteLog("[INFO] Остановлено.")
            RefreshCodeList()
            RefreshStatus()
        End Sub

        Private Sub ToggleBreakpoint()
            If _file.fi_code Is Nothing Then Return
            Dim idx As Integer = If(LstCode.SelectedIndex >= 0, LstCode.SelectedIndex, _currentLine)
            If idx < 0 OrElse idx >= _file.fi_code.Length Then Return

            If _breakpoints.Contains(idx) Then
                _breakpoints.Remove(idx)
                WriteLog($"[BP] Удалена @{idx}")
            Else
                _breakpoints.Add(idx)
                WriteLog($"[BP] Добавлена @{idx}")
            End If

            RefreshCodeList()
            RefreshStatus()
        End Sub

        Private Sub ClearBreakpoints()
            _breakpoints.Clear()
            WriteLog("[BP] Все точки удалены.")
            RefreshCodeList()
            RefreshStatus()
        End Sub

        Private Sub SetState(newState As DebuggerState)
            _state = newState
        End Sub

        ' ─────────────────────────── Обработчики меню ───────────────────────────

        Private Sub MenuFileLoad_Click(sender As Object, e As EventArgs) Handles MenuFileLoad.Click
            MessageBox.Show("Загрузка FileStruct извне будет реализована в вашем проекте.",
                            "Debugger", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Private Sub MenuFileExit_Click(sender As Object, e As EventArgs) Handles MenuFileExit.Click
            Me.Close()
        End Sub

        Private Sub MenuDebugStart_Click(sender As Object, e As EventArgs) Handles MenuDebugStart.Click
            StartDebug()
        End Sub

        Private Sub MenuDebugStep_Click(sender As Object, e As EventArgs) Handles MenuDebugStep.Click
            StepOver()
        End Sub

        Private Sub MenuDebugRun_Click(sender As Object, e As EventArgs) Handles MenuDebugRun.Click
            RunToEnd()
        End Sub

        Private Sub MenuDebugPause_Click(sender As Object, e As EventArgs) Handles MenuDebugPause.Click
            PauseDebug()
        End Sub

        Private Sub MenuDebugStop_Click(sender As Object, e As EventArgs) Handles MenuDebugStop.Click
            StopDebug()
        End Sub

        Private Sub MenuDebugClearBp_Click(sender As Object, e As EventArgs) Handles MenuDebugClearBp.Click
            ClearBreakpoints()
        End Sub

        ' ─────────────────────────── Обработчики тулбара ───────────────────────────

        Private Sub BtnStart_Click(sender As Object, e As EventArgs) Handles BtnStart.Click
            StartDebug()
        End Sub

        Private Sub BtnStep_Click(sender As Object, e As EventArgs) Handles BtnStep.Click
            StepOver()
        End Sub

        Private Sub BtnRun_Click(sender As Object, e As EventArgs) Handles BtnRun.Click
            RunToEnd()
        End Sub

        Private Sub BtnPause_Click(sender As Object, e As EventArgs) Handles BtnPause.Click
            PauseDebug()
        End Sub

        Private Sub BtnStop_Click(sender As Object, e As EventArgs) Handles BtnStop.Click
            StopDebug()
        End Sub

        Private Sub BtnToggleBp_Click(sender As Object, e As EventArgs) Handles BtnToggleBp.Click
            ToggleBreakpoint()
        End Sub

        Private Sub BtnClearBp_Click(sender As Object, e As EventArgs) Handles BtnClearBp.Click
            ClearBreakpoints()
        End Sub

        Private Sub BtnClearLog_Click(sender As Object, e As EventArgs) Handles BtnClearLog.Click
            _log.Clear()
            TxtLog.Clear()
        End Sub

        ' ─────────────────────────── Прочее ───────────────────────────

        Private Sub LstCode_DoubleClick(sender As Object, e As EventArgs) Handles LstCode.DoubleClick
            ToggleBreakpoint()
        End Sub

    End Class

End Namespace