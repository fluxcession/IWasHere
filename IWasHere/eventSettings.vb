Imports Newtonsoft.Json.Linq
Public Class eventSettings
    Private simpleSettings As New Dictionary(Of String, String)
    Private complexSettingsBySeq As New Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of String, String)))

    ''' <summary>
    ''' subsetting:
    ''' 
    ''' n1roomtoset | dictionary (Of String, x)
    '''     roomset id: RX1 | dictionary x (Of String, y)
    '''         sequence: 1 | dictionary y (Of Integer, String)
    '''             id: ROOOM1 | String in dictionary y
    '''         sequence: 2
    '''             id: ROOOM5
    '''         sequence: 3
    '''     roomset id: RX3
    ''' </summary>
    Private subSettingsByKey As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, String)))

    Public Function getEventSetting(settingName As String) As String
        Dim result As String = Nothing
        If simpleSettings(settingName) Then
            result = simpleSettings(settingName)
        End If
        Return result
    End Function

    Public Function getEventSetting(settingName As String, settingSeq As Integer, settingKey As String) As String
        Dim result As String = Nothing
        Try
            result = complexSettingsBySeq.Item(settingName).Item(settingSeq).Item(settingKey)
        Catch ex As Exception
            fluxcessLog.logMsg("Cannot get setting " + settingName + " / " + settingSeq.ToString + " / " + settingKey + ": " + ex.Message, 5)
        End Try
        Return result
    End Function

    Public Function getComplexSettingBySeq(settingName As String) As Dictionary(Of Integer, Dictionary(Of String, String))
        Dim result As Dictionary(Of Integer, Dictionary(Of String, String)) = Nothing
        Try
            result = complexSettingsBySeq(settingName)
        Catch ex As Exception
            fluxcessLog.logMsg("Cannot get complexsetting " + settingName + ": " + ex.Message, 5)
        End Try
        Return result
    End Function
    Public Function getSubSettingBySettingname(settingName As String) As Dictionary(Of String, Dictionary(Of Integer, String))
        Dim result As Dictionary(Of String, Dictionary(Of Integer, String)) = Nothing
        Try
            result = subSettingsByKey(settingName)
        Catch ex As Exception
            fluxcessLog.logMsg("Cannot get subsetting " + settingName + ": " + ex.Message, 5)
        End Try
        Return result
    End Function
    Public Function countSettingsItems(multiname As String) As Integer
        Dim result As Integer = Nothing
        Try
            result = complexSettingsBySeq(multiname).Count
        Catch ex As Exception
            fluxcessLog.logMsg("Cannot count items for multiname " + multiname + ": " + ex.Message, 5)
        End Try
        Return result
    End Function

    Public Sub loadSettings(data As JObject)
        Dim lastKey As String = ""
        Try

            For Each row In data
                lastKey = row.Key.ToString
                simpleSettings.Add(lastKey, row.Value.ToString)

                ' is this something of format name_seq_key ?
                Dim splt = row.Key.ToString.Split("_")
                If splt(0) = "n1roomtoset" Then
                    addSubSetting(splt, row.Value.ToString)
                ElseIf splt.Count > 2 Then
                    addComplexSetting(splt, row.Value.ToString)
                End If
            Next

        Catch ex As Exception
            fluxcessLog.logMsg("Problem loading settings from JObject (" + lastKey + "): " + ex.Message, 5)
        End Try
    End Sub

    Private Sub addComplexSetting(key() As String, val As String)
        Try
            If complexSettingsBySeq.ContainsKey(key(0)) Then
                If (complexSettingsBySeq.Item(key(0)).ContainsKey(Convert.ToInt32(key(1)))) Then
                    complexSettingsBySeq.Item(key(0)).Item(Convert.ToInt32(key(1))).Add(key(2), val)
                Else
                    Dim newDic As New Dictionary(Of String, String)
                    newDic.Add(key(2), val)
                    complexSettingsBySeq.Item(key(0)).Add(Convert.ToInt32(key(1)), newDic)
                End If
            Else
                Dim newDic As New Dictionary(Of String, String)
                newDic.Add(key(2), val)
                Dim noDic As New Dictionary(Of Integer, Dictionary(Of String, String))
                noDic.Add(Convert.ToInt32(key(1)), newDic)
                complexSettingsBySeq.Add(key(0), noDic)
            End If

        Catch ex As Exception
            fluxcessLog.logMsg("Problem adding complex key of length " + key.Length.ToString + " (" + key(0).ToString + " / " + key(1).ToString + ") to settings: " + ex.Message, 5)
        End Try
    End Sub

    Private Sub addSubSetting(key() As String, val As String)
        Try
            If subSettingsByKey.ContainsKey(key(0)) Then
                If (subSettingsByKey.Item(key(0)).ContainsKey(key(1))) Then
                    subSettingsByKey.Item(key(0)).Item(key(1)).Add(key(2), val)
                Else
                    Dim newDic As New Dictionary(Of Integer, String)
                    newDic.Add(Convert.ToInt32(key(2)), val)
                    subSettingsByKey.Item(key(0)).Add(key(1), newDic)
                End If
            Else
                Dim newDic As New Dictionary(Of Integer, String)
                newDic.Add(Convert.ToInt32(key(2)), val)
                Dim noDic As New Dictionary(Of String, Dictionary(Of Integer, String))
                noDic.Add(key(1), newDic)
                subSettingsByKey.Add(key(0), noDic)
            End If

        Catch ex As Exception
            fluxcessLog.logMsg("Problem adding sub complex key of length " + key.Length.ToString + " (" + key(0).ToString + " / " + key(1).ToString + ") to settings: " + ex.Message, 5)
        End Try
    End Sub

End Class
