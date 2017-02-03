Imports System.IO
''' <summary>
''' fluxcess logging class
''' </summary>
Public Class fluxcessLog
    ''' <summary>
    ''' Logs a message to the daily log file
    ''' </summary>
    ''' <param name="msg">Message to be logged</param>
    ''' <param name="level">Log Level: 1 = Error, 2-10 = Warning, > 10 = Info</param>
    Public Shared Sub logMsg(msg As String, level As Integer)
        Dim logpath As String = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)
        If logpath.Substring(logpath.Length - 1, 1) <> "\" Then
            logpath += "\"
        End If
        logpath += "fluxcess_GmbH\IWasHere\log\"
        If Not System.IO.Directory.Exists(logpath) Then
            MkDir(logpath)
        End If
        Dim w As StreamWriter = File.AppendText(logpath + "log-" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt")
        Dim logline As String = DateTime.Now.ToLongTimeString() + " " + DateTime.Now.ToLongDateString()
        If (level = 1) Then
            logline += " *ERROR* "
        ElseIf level < 11 Then
            logline += " *WARNING* "
        Else
            logline += " *INFO* "
        End If
        logline += msg
        w.WriteLine(logline)
        w.Close()
    End Sub
End Class
