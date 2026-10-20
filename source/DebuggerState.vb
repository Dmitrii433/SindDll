' ─────────────────────────────────────────────────────────────────────────────
'  DebuggerState.vb
'  Namespace Debug — состояние отладчика.
' ─────────────────────────────────────────────────────────────────────────────

Namespace Debug

    ''' <summary>Состояние отладчика.</summary>
    Public Enum DebuggerState
        Idle
        Running
        Paused
        Stopped
        Finished
    End Enum

End Namespace