Public Structure FileStruct
    Public fi_apptype As AppType
    Public fi_entrypoint As NativeString
    Public fi_code As NativeArray(Of NativeString)
    Public fi_icon As Resource
    Public fi_debugenabled As Bool
End Structure
