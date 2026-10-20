' ─────────────────────────────────────────────────────────────────────────────
'  Debugger.Designer.vb
'  Namespace Debug — WinForms-дизайнер.
' ─────────────────────────────────────────────────────────────────────────────

Imports System.Windows.Forms
Imports System.Drawing

Namespace Debug

    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class Debugger
        Inherits System.Windows.Forms.Form

        <System.Diagnostics.DebuggerNonUserCode()>
        Protected Overrides Sub Dispose(disposing As Boolean)
            Try
                If disposing AndAlso components IsNot Nothing Then
                    components.Dispose()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub

        Private components As System.ComponentModel.IContainer

        ' ── Контролы ──
        Friend WithEvents MenuStrip1 As System.Windows.Forms.MenuStrip
        Friend WithEvents MenuFile As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MenuFileLoad As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MenuFileExit As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MenuDebug As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MenuDebugStart As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MenuDebugStep As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MenuDebugRun As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MenuDebugPause As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MenuDebugStop As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MenuDebugClearBp As System.Windows.Forms.ToolStripMenuItem

        Friend WithEvents ToolStrip1 As System.Windows.Forms.ToolStrip
        Friend WithEvents BtnStart As System.Windows.Forms.ToolStripButton
        Friend WithEvents BtnStep As System.Windows.Forms.ToolStripButton
        Friend WithEvents BtnRun As System.Windows.Forms.ToolStripButton
        Friend WithEvents BtnPause As System.Windows.Forms.ToolStripButton
        Friend WithEvents BtnStop As System.Windows.Forms.ToolStripButton
        Friend WithEvents Sep1 As System.Windows.Forms.ToolStripSeparator
        Friend WithEvents BtnToggleBp As System.Windows.Forms.ToolStripButton
        Friend WithEvents BtnClearBp As System.Windows.Forms.ToolStripButton
        Friend WithEvents Sep2 As System.Windows.Forms.ToolStripSeparator
        Friend WithEvents BtnClearLog As System.Windows.Forms.ToolStripButton

        Friend WithEvents SplitMain As System.Windows.Forms.SplitContainer
        Friend WithEvents SplitRight As System.Windows.Forms.SplitContainer

        Friend WithEvents LstCode As System.Windows.Forms.ListBox
        Friend WithEvents GrpWatches As System.Windows.Forms.GroupBox
        Friend WithEvents LstWatches As System.Windows.Forms.ListView
        Friend WithEvents ColWatchName As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColWatchValue As System.Windows.Forms.ColumnHeader
        Friend WithEvents GrpLog As System.Windows.Forms.GroupBox
        Friend WithEvents TxtLog As System.Windows.Forms.TextBox

        Friend WithEvents StatusStrip1 As System.Windows.Forms.StatusStrip
        Friend WithEvents LblState As System.Windows.Forms.ToolStripStatusLabel
        Friend WithEvents LblLine As System.Windows.Forms.ToolStripStatusLabel
        Friend WithEvents LblBp As System.Windows.Forms.ToolStripStatusLabel

        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
            Me.MenuFile = New System.Windows.Forms.ToolStripMenuItem()
            Me.MenuFileLoad = New System.Windows.Forms.ToolStripMenuItem()
            Me.MenuFileExit = New System.Windows.Forms.ToolStripMenuItem()
            Me.MenuDebug = New System.Windows.Forms.ToolStripMenuItem()
            Me.MenuDebugStart = New System.Windows.Forms.ToolStripMenuItem()
            Me.MenuDebugStep = New System.Windows.Forms.ToolStripMenuItem()
            Me.MenuDebugRun = New System.Windows.Forms.ToolStripMenuItem()
            Me.MenuDebugPause = New System.Windows.Forms.ToolStripMenuItem()
            Me.MenuDebugStop = New System.Windows.Forms.ToolStripMenuItem()
            Me.MenuDebugClearBp = New System.Windows.Forms.ToolStripMenuItem()
            Me.ToolStrip1 = New System.Windows.Forms.ToolStrip()
            Me.BtnStart = New System.Windows.Forms.ToolStripButton()
            Me.BtnStep = New System.Windows.Forms.ToolStripButton()
            Me.BtnRun = New System.Windows.Forms.ToolStripButton()
            Me.BtnPause = New System.Windows.Forms.ToolStripButton()
            Me.BtnStop = New System.Windows.Forms.ToolStripButton()
            Me.Sep1 = New System.Windows.Forms.ToolStripSeparator()
            Me.BtnToggleBp = New System.Windows.Forms.ToolStripButton()
            Me.BtnClearBp = New System.Windows.Forms.ToolStripButton()
            Me.Sep2 = New System.Windows.Forms.ToolStripSeparator()
            Me.BtnClearLog = New System.Windows.Forms.ToolStripButton()
            Me.SplitMain = New System.Windows.Forms.SplitContainer()
            Me.LstCode = New System.Windows.Forms.ListBox()
            Me.SplitRight = New System.Windows.Forms.SplitContainer()
            Me.GrpWatches = New System.Windows.Forms.GroupBox()
            Me.LstWatches = New System.Windows.Forms.ListView()
            Me.ColWatchName = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
            Me.ColWatchValue = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
            Me.GrpLog = New System.Windows.Forms.GroupBox()
            Me.TxtLog = New System.Windows.Forms.TextBox()
            Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
            Me.LblState = New System.Windows.Forms.ToolStripStatusLabel()
            Me.LblLine = New System.Windows.Forms.ToolStripStatusLabel()
            Me.LblBp = New System.Windows.Forms.ToolStripStatusLabel()
            Me.MenuStrip1.SuspendLayout()
            Me.ToolStrip1.SuspendLayout()
            CType(Me.SplitMain, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SplitMain.Panel1.SuspendLayout()
            Me.SplitMain.Panel2.SuspendLayout()
            Me.SplitMain.SuspendLayout()
            CType(Me.SplitRight, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SplitRight.Panel1.SuspendLayout()
            Me.SplitRight.Panel2.SuspendLayout()
            Me.SplitRight.SuspendLayout()
            Me.GrpWatches.SuspendLayout()
            Me.GrpLog.SuspendLayout()
            Me.StatusStrip1.SuspendLayout()
            Me.SuspendLayout()
            '
            'MenuStrip1
            '
            Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.MenuFile, Me.MenuDebug})
            Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
            Me.MenuStrip1.Name = "MenuStrip1"
            Me.MenuStrip1.Size = New System.Drawing.Size(900, 24)
            Me.MenuStrip1.TabIndex = 0
            '
            'MenuFile
            '
            Me.MenuFile.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.MenuFileLoad, Me.MenuFileExit})
            Me.MenuFile.Name = "MenuFile"
            Me.MenuFile.Size = New System.Drawing.Size(48, 20)
            Me.MenuFile.Text = "&Файл"
            '
            'MenuFileLoad
            '
            Me.MenuFileLoad.Name = "MenuFileLoad"
            Me.MenuFileLoad.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.O), System.Windows.Forms.Keys)
            Me.MenuFileLoad.Size = New System.Drawing.Size(240, 22)
            Me.MenuFileLoad.Text = "&Загрузить FileStruct..."
            '
            'MenuFileExit
            '
            Me.MenuFileExit.Name = "MenuFileExit"
            Me.MenuFileExit.ShortcutKeys = CType((System.Windows.Forms.Keys.Alt Or System.Windows.Forms.Keys.F4), System.Windows.Forms.Keys)
            Me.MenuFileExit.Size = New System.Drawing.Size(240, 22)
            Me.MenuFileExit.Text = "В&ыход"
            '
            'MenuDebug
            '
            Me.MenuDebug.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.MenuDebugStart, Me.MenuDebugStep, Me.MenuDebugRun, Me.MenuDebugPause, Me.MenuDebugStop, Me.MenuDebugClearBp})
            Me.MenuDebug.Name = "MenuDebug"
            Me.MenuDebug.Size = New System.Drawing.Size(64, 20)
            Me.MenuDebug.Text = "&Отладка"
            '
            'MenuDebugStart
            '
            Me.MenuDebugStart.Name = "MenuDebugStart"
            Me.MenuDebugStart.ShortcutKeys = System.Windows.Forms.Keys.F5
            Me.MenuDebugStart.Size = New System.Drawing.Size(214, 22)
            Me.MenuDebugStart.Text = "&Старт"
            '
            'MenuDebugStep
            '
            Me.MenuDebugStep.Name = "MenuDebugStep"
            Me.MenuDebugStep.ShortcutKeys = System.Windows.Forms.Keys.F10
            Me.MenuDebugStep.Size = New System.Drawing.Size(214, 22)
            Me.MenuDebugStep.Text = "&Шаг (Step Over)"
            '
            'MenuDebugRun
            '
            Me.MenuDebugRun.Name = "MenuDebugRun"
            Me.MenuDebugRun.ShortcutKeys = System.Windows.Forms.Keys.F11
            Me.MenuDebugRun.Size = New System.Drawing.Size(214, 22)
            Me.MenuDebugRun.Text = "&Выполнить до конца"
            '
            'MenuDebugPause
            '
            Me.MenuDebugPause.Name = "MenuDebugPause"
            Me.MenuDebugPause.ShortcutKeys = System.Windows.Forms.Keys.F6
            Me.MenuDebugPause.Size = New System.Drawing.Size(214, 22)
            Me.MenuDebugPause.Text = "&Пауза"
            '
            'MenuDebugStop
            '
            Me.MenuDebugStop.Name = "MenuDebugStop"
            Me.MenuDebugStop.ShortcutKeys = CType((System.Windows.Forms.Keys.Shift Or System.Windows.Forms.Keys.F5), System.Windows.Forms.Keys)
            Me.MenuDebugStop.Size = New System.Drawing.Size(214, 22)
            Me.MenuDebugStop.Text = "С&топ"
            '
            'MenuDebugClearBp
            '
            Me.MenuDebugClearBp.Name = "MenuDebugClearBp"
            Me.MenuDebugClearBp.Size = New System.Drawing.Size(214, 22)
            Me.MenuDebugClearBp.Text = "Очистить &точки останова"
            '
            'ToolStrip1
            '
            Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.BtnStart, Me.BtnStep, Me.BtnRun, Me.BtnPause, Me.BtnStop, Me.Sep1, Me.BtnToggleBp, Me.BtnClearBp, Me.Sep2, Me.BtnClearLog})
            Me.ToolStrip1.Location = New System.Drawing.Point(0, 24)
            Me.ToolStrip1.Name = "ToolStrip1"
            Me.ToolStrip1.Size = New System.Drawing.Size(900, 25)
            Me.ToolStrip1.TabIndex = 1
            '
            'BtnStart
            '
            Me.BtnStart.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.BtnStart.Name = "BtnStart"
            Me.BtnStart.Size = New System.Drawing.Size(55, 22)
            Me.BtnStart.Text = "▶ Старт"
            Me.BtnStart.ToolTipText = "Начать отладку (F5)"
            '
            'BtnStep
            '
            Me.BtnStep.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.BtnStep.Name = "BtnStep"
            Me.BtnStep.Size = New System.Drawing.Size(47, 22)
            Me.BtnStep.Text = "⇥ Шаг"
            Me.BtnStep.ToolTipText = "Шаг (F10)"
            '
            'BtnRun
            '
            Me.BtnRun.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.BtnRun.Name = "BtnRun"
            Me.BtnRun.Size = New System.Drawing.Size(82, 22)
            Me.BtnRun.Text = "» Выполнить"
            Me.BtnRun.ToolTipText = "Выполнить до конца (F11)"
            '
            'BtnPause
            '
            Me.BtnPause.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.BtnPause.Name = "BtnPause"
            Me.BtnPause.Size = New System.Drawing.Size(56, 22)
            Me.BtnPause.Text = "❚❚ Пауза"
            Me.BtnPause.ToolTipText = "Пауза (F6)"
            '
            'BtnStop
            '
            Me.BtnStop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.BtnStop.Name = "BtnStop"
            Me.BtnStop.Size = New System.Drawing.Size(51, 22)
            Me.BtnStop.Text = "■ Стоп"
            Me.BtnStop.ToolTipText = "Остановить (Shift+F5)"
            '
            'Sep1
            '
            Me.Sep1.Name = "Sep1"
            Me.Sep1.Size = New System.Drawing.Size(6, 25)
            '
            'BtnToggleBp
            '
            Me.BtnToggleBp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.BtnToggleBp.Name = "BtnToggleBp"
            Me.BtnToggleBp.Size = New System.Drawing.Size(78, 22)
            Me.BtnToggleBp.Text = "● Breakpoint"
            Me.BtnToggleBp.ToolTipText = "Переключить точку останова на текущей строке"
            '
            'BtnClearBp
            '
            Me.BtnClearBp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.BtnClearBp.Name = "BtnClearBp"
            Me.BtnClearBp.Size = New System.Drawing.Size(60, 22)
            Me.BtnClearBp.Text = "✕ Все BP"
            Me.BtnClearBp.ToolTipText = "Очистить все точки останова"
            '
            'Sep2
            '
            Me.Sep2.Name = "Sep2"
            Me.Sep2.Size = New System.Drawing.Size(6, 25)
            '
            'BtnClearLog
            '
            Me.BtnClearLog.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
            Me.BtnClearLog.Name = "BtnClearLog"
            Me.BtnClearLog.Size = New System.Drawing.Size(85, 22)
            Me.BtnClearLog.Text = "Очистить лог"
            Me.BtnClearLog.ToolTipText = "Очистить лог отладчика"
            '
            'SplitMain
            '
            Me.SplitMain.Dock = System.Windows.Forms.DockStyle.Fill
            Me.SplitMain.Location = New System.Drawing.Point(0, 49)
            Me.SplitMain.Name = "SplitMain"
            '
            'SplitMain.Panel1
            '
            Me.SplitMain.Panel1.Controls.Add(Me.LstCode)
            '
            'SplitMain.Panel2
            '
            Me.SplitMain.Panel2.Controls.Add(Me.SplitRight)
            Me.SplitMain.Size = New System.Drawing.Size(900, 529)
            Me.SplitMain.SplitterDistance = 500
            Me.SplitMain.TabIndex = 2
            '
            'LstCode
            '
            Me.LstCode.Dock = System.Windows.Forms.DockStyle.Fill
            Me.LstCode.Font = New System.Drawing.Font("Consolas", 10.0!)
            Me.LstCode.IntegralHeight = False
            Me.LstCode.ItemHeight = 15
            Me.LstCode.Location = New System.Drawing.Point(0, 0)
            Me.LstCode.Name = "LstCode"
            Me.LstCode.Size = New System.Drawing.Size(500, 529)
            Me.LstCode.TabIndex = 0
            '
            'SplitRight
            '
            Me.SplitRight.Dock = System.Windows.Forms.DockStyle.Fill
            Me.SplitRight.Location = New System.Drawing.Point(0, 0)
            Me.SplitRight.Name = "SplitRight"
            Me.SplitRight.Orientation = System.Windows.Forms.Orientation.Horizontal
            '
            'SplitRight.Panel1
            '
            Me.SplitRight.Panel1.Controls.Add(Me.GrpWatches)
            '
            'SplitRight.Panel2
            '
            Me.SplitRight.Panel2.Controls.Add(Me.GrpLog)
            Me.SplitRight.Size = New System.Drawing.Size(396, 529)
            Me.SplitRight.SplitterDistance = 375
            Me.SplitRight.TabIndex = 1
            '
            'GrpWatches
            '
            Me.GrpWatches.Controls.Add(Me.LstWatches)
            Me.GrpWatches.Dock = System.Windows.Forms.DockStyle.Fill
            Me.GrpWatches.Location = New System.Drawing.Point(0, 0)
            Me.GrpWatches.Name = "GrpWatches"
            Me.GrpWatches.Padding = New System.Windows.Forms.Padding(6)
            Me.GrpWatches.Size = New System.Drawing.Size(396, 375)
            Me.GrpWatches.TabIndex = 0
            Me.GrpWatches.TabStop = False
            Me.GrpWatches.Text = "Watch (регистры/флаги)"
            '
            'LstWatches
            '
            Me.LstWatches.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColWatchName, Me.ColWatchValue})
            Me.LstWatches.Dock = System.Windows.Forms.DockStyle.Fill
            Me.LstWatches.FullRowSelect = True
            Me.LstWatches.GridLines = True
            Me.LstWatches.HideSelection = False
            Me.LstWatches.Location = New System.Drawing.Point(6, 19)
            Me.LstWatches.Name = "LstWatches"
            Me.LstWatches.Size = New System.Drawing.Size(384, 350)
            Me.LstWatches.TabIndex = 0
            Me.LstWatches.UseCompatibleStateImageBehavior = False
            Me.LstWatches.View = System.Windows.Forms.View.Details
            '
            'ColWatchName
            '
            Me.ColWatchName.Text = "Имя"
            Me.ColWatchName.Width = 180
            '
            'ColWatchValue
            '
            Me.ColWatchValue.Text = "Значение"
            Me.ColWatchValue.Width = 260
            '
            'GrpLog
            '
            Me.GrpLog.Controls.Add(Me.TxtLog)
            Me.GrpLog.Dock = System.Windows.Forms.DockStyle.Fill
            Me.GrpLog.Location = New System.Drawing.Point(0, 0)
            Me.GrpLog.Name = "GrpLog"
            Me.GrpLog.Padding = New System.Windows.Forms.Padding(6)
            Me.GrpLog.Size = New System.Drawing.Size(396, 150)
            Me.GrpLog.TabIndex = 0
            Me.GrpLog.TabStop = False
            Me.GrpLog.Text = "Лог"
            '
            'TxtLog
            '
            Me.TxtLog.Dock = System.Windows.Forms.DockStyle.Fill
            Me.TxtLog.Font = New System.Drawing.Font("Consolas", 9.0!)
            Me.TxtLog.Location = New System.Drawing.Point(6, 19)
            Me.TxtLog.Multiline = True
            Me.TxtLog.Name = "TxtLog"
            Me.TxtLog.ReadOnly = True
            Me.TxtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both
            Me.TxtLog.Size = New System.Drawing.Size(384, 125)
            Me.TxtLog.TabIndex = 0
            Me.TxtLog.WordWrap = False
            '
            'StatusStrip1
            '
            Me.StatusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.LblState, Me.LblLine, Me.LblBp})
            Me.StatusStrip1.Location = New System.Drawing.Point(0, 578)
            Me.StatusStrip1.Name = "StatusStrip1"
            Me.StatusStrip1.Size = New System.Drawing.Size(900, 22)
            Me.StatusStrip1.TabIndex = 3
            '
            'LblState
            '
            Me.LblState.Name = "LblState"
            Me.LblState.Size = New System.Drawing.Size(91, 17)
            Me.LblState.Text = "Состояние: Idle"
            '
            'LblLine
            '
            Me.LblLine.Name = "LblLine"
            Me.LblLine.Size = New System.Drawing.Size(57, 17)
            Me.LblLine.Text = "Строка: -"
            '
            'LblBp
            '
            Me.LblBp.Name = "LblBp"
            Me.LblBp.Size = New System.Drawing.Size(33, 17)
            Me.LblBp.Text = "BP: 0"
            '
            'Debugger
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(900, 600)
            Me.Controls.Add(Me.SplitMain)
            Me.Controls.Add(Me.StatusStrip1)
            Me.Controls.Add(Me.ToolStrip1)
            Me.Controls.Add(Me.MenuStrip1)
            Me.MainMenuStrip = Me.MenuStrip1
            Me.MinimumSize = New System.Drawing.Size(600, 400)
            Me.Name = "Debugger"
            Me.ShowIcon = False
            Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
            Me.Text = "Sind Debugger"
            Me.MenuStrip1.ResumeLayout(False)
            Me.MenuStrip1.PerformLayout()
            Me.ToolStrip1.ResumeLayout(False)
            Me.ToolStrip1.PerformLayout()
            Me.SplitMain.Panel1.ResumeLayout(False)
            Me.SplitMain.Panel2.ResumeLayout(False)
            CType(Me.SplitMain, System.ComponentModel.ISupportInitialize).EndInit()
            Me.SplitMain.ResumeLayout(False)
            Me.SplitRight.Panel1.ResumeLayout(False)
            Me.SplitRight.Panel2.ResumeLayout(False)
            CType(Me.SplitRight, System.ComponentModel.ISupportInitialize).EndInit()
            Me.SplitRight.ResumeLayout(False)
            Me.GrpWatches.ResumeLayout(False)
            Me.GrpLog.ResumeLayout(False)
            Me.GrpLog.PerformLayout()
            Me.StatusStrip1.ResumeLayout(False)
            Me.StatusStrip1.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

    End Class

End Namespace