Imports System.Data
Imports System.Net
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Web
Imports System.Text
Imports System.IO

Imports System.Printing
Imports System.Windows.Documents
Imports System.Xml
Imports System.Windows.Markup
Imports System.Globalization
Imports System.Windows.Threading
'Imports System.Drawing

Class MainWindow
    Dim timeoutms As Integer = 9000

    Dim newTable As New DataTable
    Dim lastSearchBoxContent As String = ""
    Dim isPopupOpen As Boolean = False
    Dim sessionnames As Dictionary(Of String, Boolean) = New Dictionary(Of String, Boolean)
    Dim currentGuestId As Integer = 0F
    Dim currentGuestStatus As String = ""

    Dim eventIdInitiallyRead As Boolean = False

    ' für windows forms druck
    Dim WithEvents prndoc As System.Drawing.Printing.PrintDocument = New System.Drawing.Printing.PrintDocument
    Dim printFont1 As System.Drawing.Font
    Dim printFont2 As System.Drawing.Font
    Dim printFont3 As System.Drawing.Font

    ' für druck (inhalte)
    Dim ticketGuestName As String = ""
    Dim ticketGuestCompany As String = ""
    Dim ticketGuestGroups As String = ""
    Dim ticketGuestWebcode As String = ""

    ''' <summary>
    ''' left, right, top, bottom
    ''' </summary>
    Dim ticketmargins As System.Drawing.Printing.Margins = New System.Drawing.Printing.Margins(0, 50, 10, 20)

    Dim myimg As New System.Drawing.Bitmap(1500, 820)
    Dim Font1Size As Integer = 30
    Dim Font2Size As Integer = 20
    Dim Font3Size As Integer = 40
    Dim Font1Typo As String = "Arial"
    Dim Font2Typo As String = "Arial"
    Dim Font3Typo As String = "Arial"

    Dim ticketPageWidth As Integer = 1500
    Dim ticketPageHeight As Integer = 800

    Dim WithEvents dt As DispatcherTimer = New DispatcherTimer
    Dim WithEvents popupCloseTimer As DispatcherTimer = New DispatcherTimer

    Dim roomSets As Dictionary(Of String, String)
    Dim roomsInRoomSets As Dictionary(Of String, Dictionary(Of String, String))
    Dim allRoomIds As Dictionary(Of Integer, String)
    Dim allRoomTitles As Dictionary(Of Integer, String)

    Dim allDataFields As Dictionary(Of Integer, String)

    Dim allEventIds As Dictionary(Of Integer, String)
    Dim allEventNames As Dictionary(Of Integer, String)

    Dim eventSettings As eventSettings

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        loadSettings()
        ' dynamic colors of the application frontend
        designTheApp()
        textBoxSearchEx.Focus()
        ' Hide the settings window
        ccPopup.Visibility = Visibility.Hidden

        Try
            If My.Settings.onoffline = "online" Then
                loadStats()
            ElseIf My.Settings.onoffline = "online2" Then
                loadStats2()
            End If
            ' the timer is used to check the statistics in regular intervals
            dt.Interval = New TimeSpan(0, 0, 10)
            dt.Start()
        Catch ex As Exception
            fluxcessLog.logMsg("Loading statistics or timer start failed: " + ex.Message, 1)
        End Try
    End Sub

    ''' <summary>
    ''' sets GUI colors according to the application-wide settings
    ''' </summary>
    Private Sub designTheApp()
        Dim bc = New BrushConverter()
        If My.Settings.bgcolor.Length = 6 Then
            My.Settings.bgcolor = "FF" + My.Settings.bgcolor
        End If
        If My.Settings.forecolor.Length = 6 Then
            My.Settings.forecolor = "FF" + My.Settings.forecolor
        End If

        Dim bgcolor = bc.ConvertFrom("#" + My.Settings.bgcolor)
        Dim forecolor = bc.ConvertFrom("#" + My.Settings.forecolor)
        Dim popupbgcolor = bc.ConvertFrom("#" + My.Settings.popupbackgroundcolor)
        Dim popupfgcolor = bc.ConvertFrom("#" + My.Settings.popupforegroundcolor)
        gridMain.Background = bgcolor
        textBoxEventTitle.Foreground = forecolor
        labelDefaultMessage.Foreground = forecolor
        labelTotalCounter.Foreground = forecolor

        ccPopupGrid.Background = popupbgcolor
        labelDanke.Foreground = popupfgcolor
        labelGuestCompany.Foreground = popupfgcolor
        labelGuestName.Foreground = popupfgcolor
        labelZaehlt.Foreground = popupfgcolor
    End Sub

    ''' <summary>
    ''' close the popup and stop the popup-closer-timer
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub popupclosetimer_Tick(sender As Object, e As EventArgs) Handles popupCloseTimer.Tick
        closePopup()
        popupCloseTimer.Stop()
    End Sub


    Private Sub searchForPersons(searchex As String)
        Try
            Dim searchexencoded As String = HttpUtility.UrlEncode(searchex, System.Text.Encoding.ASCII)
            Dim url As String = My.Settings.checkinserver + "/aj/searchjson"
            Dim request As WebRequest = WebRequest.Create(url)
            request.Method = "POST"
            ' Create POST data and convert it to a byte array.
            Dim postData As String = "reqid=5&fevent_id=" + My.Settings.eventid.ToString + "&searchex=" + searchex
            Dim byteArray As Byte() = Encoding.UTF8.GetBytes(postData)
            request.ContentType = "application/x-www-form-urlencoded"
            request.ContentLength = byteArray.Length
            Dim dataStream As Stream = request.GetRequestStream()
            dataStream.Write(byteArray, 0, byteArray.Length)
            dataStream.Close()
            ' Get the response.
            Dim response As WebResponse = request.GetResponse()
            dataStream = response.GetResponseStream()
            Dim reader As New StreamReader(dataStream)
            Dim content As String = reader.ReadToEnd()
            reader.Close()
            dataStream.Close()
            response.Close()

            Dim result As JObject = JObject.Parse(content)

            If (result.Item("fehler") = "-") Then
                newTable.Clear()
                newTable.Columns.Clear()
                newTable.Columns.Add("Guest Id")
                newTable.Columns.Add("Guest Name")
                newTable.Columns.Add("Firma")
                Dim recs = result.Item("inhalt")

                For i = 0 To recs.Count - 1
                    Dim guestid = recs.Item(i).Item("guest_id")
                    Dim guestname As String = ""
                    guestname += recs.Item(i).Item("salutation").ToString + " "
                    If recs.Item(i).Item("title") Is Nothing Then
                    Else
                        If (recs.Item(i).Item("title").ToString.Length > 0) Then
                            guestname += recs.Item(i).Item("title").ToString + " "
                        End If
                    End If
                    guestname += recs.Item(i).Item("firstname").ToString + " "
                    guestname += recs.Item(i).Item("lastname").ToString
                    Dim guestcompany As String = recs.Item(i).Item("company").ToString
                    newTable.Rows.Add(guestid, guestname, guestcompany)

                Next i
                'DataGridView1.DataSource = newTable
                'dataGridSearchResults.DataContext = newTable
                '        dataGridSearchResults.ItemsSource = newTable
            Else
                'RichTextBox1.Text = result.ToString
            End If
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

    End Sub

    Private Sub showGuestPopupFromGuestId(guest_id)
        currentGuestId = Nothing 'guest_id
        'LabelGuestId.Text = guest_id.ToString

        'Panel1.Show()
        'Panel1.Dock = DockStyle.Fill
        'Panel1.Focus()
        Try

            Dim url As String = My.Settings.checkinserver + "/aj/detailsjson?fevent_id=" + My.Settings.eventid.ToString + "&id=" + guest_id.ToString
            Dim myClient = New WebClient()

            Dim content = myClient.DownloadString(url)
            Dim result As JObject = JObject.Parse(content)

            If (result.Item("fehler") = "-") Then
                Dim rec = result.Item("inhalt")
                currentGuestId = Convert.ToInt32(result.Item("Inhalt").Item("guest_id"))
                'MsgBox(rec)
                loadPopupContent(rec)
            Else
                'MsgBox(result.ToString)
            End If

        Catch ex As Exception
            MsgBox("Fehler beim Laden der Details: " + ex.Message)
        End Try
    End Sub
    Private Sub openPopupFromWebcode(fieldname As String, fieldvalue As String)
        Dim isLogged As Boolean = False
        Try
            If My.Settings.onoffline = "offline" Then
                Try
                    logActionToFile(fieldname, fieldvalue, "", "", My.Settings.actionid, My.Settings.actiontitle)
                    isLogged = True
                Catch ex As Exception

                End Try
            End If


            If My.Settings.onoffline = "online" Then
                Try
                    Dim url As String = My.Settings.checkinserver + "/aj/detailspjson" '?reqid=5&field=webcode&value=" + webcode

                    Dim request As WebRequest = WebRequest.Create(url)
                    request.Method = "POST"
                    request.Timeout = timeoutms
                    ' Create POST data and convert it to a byte array.
                    Dim postData As String = "reqid=5&fevent_id=" + My.Settings.eventid.ToString + "&field=" + fieldname + "&value=" + fieldvalue
                    Dim byteArray As Byte() = Encoding.UTF8.GetBytes(postData)
                    request.ContentType = "application/x-www-form-urlencoded"
                    request.ContentLength = byteArray.Length
                    Dim dataStream As Stream = request.GetRequestStream()
                    dataStream.Write(byteArray, 0, byteArray.Length)
                    dataStream.Close()
                    ' Get the response.
                    Dim response As WebResponse = request.GetResponse()
                    dataStream = response.GetResponseStream()
                    Dim reader As New StreamReader(dataStream)
                    Dim content As String = reader.ReadToEnd()
                    reader.Close()
                    dataStream.Close()
                    response.Close()
                    Try
                        Dim result As JObject = JObject.Parse(content)
                        If (result.Item("fehler") = "-") And Not result.Item("inhalt").Item("guest_id") Is Nothing Then
                            Dim rec = result.Item("inhalt")
                            If Not rec.Item("guest_id") Is Nothing Then
                                currentGuestId = Convert.ToInt32(rec.Item("guest_id"))
                                loadPopupContent(rec)
                            Else
                                MsgBox("Fehler: " + fieldname + " " + fieldvalue + " nicht gefunden.")
                            End If
                            logActionToFile(fieldname, fieldvalue, rec.Item("guest_id").ToString, rec.Item("lastname").ToString, My.Settings.actionid, My.Settings.actiontitle)
                            isLogged = True
                        ElseIf result.Item("fehler") = "-" And result.Item("inhalt").Item("guest_id") Is Nothing Then
                            showGuestErrorPopup(fieldname, fieldvalue)
                        End If
                    Catch ex As Exception
                        MsgBox("Der Server hat auf die Anfrage nach Teilnehmerdetails eine unleserliche Antwort gesendet. " + ex.Message + " " + content)
                    End Try

                Catch ex As Exception

                End Try
            ElseIf My.Settings.onoffline = "online2" Then
                Dim url As String = My.Settings.adminurl + "/api/events/" + My.Settings.eventid.ToString + "/guests/" + fieldvalue + "?special=checkin"

                Dim request As WebRequest = WebRequest.Create(url)
                SetBasicAuthHeader(request, My.Settings.adminurlusername, My.Settings.adminurlpassword)
                request.Timeout = timeoutms

                'request.Credentials = CredentialCache.DefaultCredentials
                ' Get the response.
                Dim response As WebResponse = request.GetResponse()
                ' Display the status.
                Console.WriteLine(CType(response, HttpWebResponse).StatusDescription)
                ' Get the stream containing content returned by the server.
                Dim dataStream As Stream = response.GetResponseStream()
                ' Open the stream using a StreamReader for easy access.
                Dim reader As New StreamReader(dataStream)
                ' Read the content.
                Dim responseFromServer As String = reader.ReadToEnd()
                ' Display the content.
                Console.WriteLine(responseFromServer)
                ' Clean up the streams and the response.
                reader.Close()
                response.Close()

                Dim content As String = responseFromServer
                Try
                    Dim result As JObject = JObject.Parse(content)
                    If (result.Item("err") Is Nothing And Not result.Item("guest").Item("guest_id") Is Nothing) Then
                        Dim rec = result.Item("guest")
                        If Not rec.Item("guest_id") Is Nothing Then
                            currentGuestId = Convert.ToInt32(rec.Item("guest_id"))
                            loadPopupContent(rec)
                        Else
                            MsgBox("Fehler: " + fieldname + " " + fieldvalue + " nicht gefunden.")
                        End If
                    End If
                Catch ex As Exception
                    MsgBox("Der Server hat auf die Anfrage nach Teilnehmerdetails eine unleserliche Antwort gesendet. " + ex.Message + " " + content)
                End Try
            Else
                loadPopupContent(fieldvalue, 1)
            End If
        Catch ex As Exception
            MsgBox("Konnte keinen Kontakt zum Server aufnehmen. " + ex.Message)
        End Try
        If isLogged = False Then
            logActionToFile(fieldname, fieldvalue, "", "", My.Settings.actionid, My.Settings.actiontitle)
        End If
    End Sub
    Private Sub loadPopupContent(webcode As String, div1 As Integer)
        Dim bc = New BrushConverter()
        Dim bgcolor = bc.ConvertFrom("#FF" + My.Settings.bgcolor)
        ccPopupGrid.Background = bgcolor

        labelGuestName.Content = webcode
        labelGuestCompany.Content = ""
        labelDanke.Content = "Code erfasst:"

        ccPopup.Visibility = Visibility.Visible
        isPopupOpen = True

        popupCloseTimer.Interval = TimeSpan.FromSeconds(2)
        popupCloseTimer.Start()
    End Sub
    Private Sub showGuestErrorPopup(fieldname As String, fieldvalue As String)
        ccPopupGrid.Background = Brushes.DarkRed
        labelDanke.Content = "FEHLER"
        labelGuestName.Content = "Code nicht gefunden."
        labelGuestCompany.Content = ""
        labelGuestCompany.Content = "Feld: " + fieldname + " / Wert: " + fieldvalue
        labelZaehlt.Content = "Weiter mit <Escape>..."
        ccPopup.Visibility = Visibility.Visible
        isPopupOpen = True
    End Sub
    Private Sub loadPopupContent(rec As JObject)
        Dim bc = New BrushConverter()
        Dim bgcolor = bc.ConvertFrom("#" + My.Settings.popupbackgroundcolor)
        ccPopupGrid.Background = bgcolor

        Dim guestname = ""
        ticketGuestName = ""
        ticketGuestCompany = ""
        ticketGuestGroups = ""
        ticketGuestWebcode = ""
        labelDanke.Content = My.Settings.labelcontentThankyou
        'ccPopup.Background = Brushes.White

        If (Not rec.Item("status") Is Nothing) Then
            currentGuestStatus = rec.Item("status")
        Else
            currentGuestStatus = ""
        End If

        If (Not rec.Item("salutation") Is Nothing) Then
            guestname += rec.Item("salutation").ToString + " "
        End If
        If Not rec.Item("title") Is Nothing Then
            If rec.Item("title").ToString.Length > 0 Then
                guestname += rec.Item("title").ToString + " "
                ticketGuestName += rec.Item("title").ToString + " "
            End If
        End If
        If Not rec.Item("firstname") Is Nothing Then
            guestname += rec.Item("firstname").ToString + " "
            ticketGuestName += rec.Item("firstname").ToString + " "
        End If
        If Not rec.Item("lastname") Is Nothing Then
            guestname += rec.Item("lastname").ToString
            ticketGuestName += rec.Item("lastname").ToString
        End If

        labelGuestName.Content = guestname

        Dim guestcompany As String = ""
        If Not rec.Item("company") Is Nothing Then
            guestcompany = rec.Item("company").ToString
        End If
        labelGuestCompany.Content = guestcompany
        ticketGuestCompany = guestcompany

        Dim numberOfCheckins As Integer = 0
        If Not rec.Item("checkin") Is Nothing Then
            Try

                If Not rec.Item("checkin").Item(My.Settings.actionid) Is Nothing Then
                    If Not rec.Item("checkin").Item(My.Settings.actionid).Item("pax") Is Nothing Then

                        numberOfCheckins = Convert.ToInt32(rec.Item("checkin").Item(My.Settings.actionid).Item("pax")) + 1
                    End If
                End If

            Catch ex As Exception

            End Try
        End If
        If numberOfCheckins = 0 Then
            labelZaehlt.Content = "Schön, dass Sie da sind!"
        Else
            labelZaehlt.Content = "Dies ist der " + numberOfCheckins.ToString + ". Check-In heute."
        End If


        ccPopup.Visibility = Visibility.Visible
        isPopupOpen = True

        If My.Settings.applicationmode = "in" Then
            doGuestCheckinout("checkin")
        ElseIf My.Settings.applicationmode = "out" Then
            doGuestCheckinout("checkout")
        Else
            addGuestAction()
        End If

        popupCloseTimer.Interval = TimeSpan.FromSeconds(3)
        popupCloseTimer.Start()
    End Sub
    Private Sub loadStats2()
        Try

            Dim url As String = My.Settings.adminurl + "/api/events/" + My.Settings.eventid.ToString + "/checkinstats"
            'MsgBox(url)
            Dim request As WebRequest = WebRequest.Create(url)
            SetBasicAuthHeader(request, My.Settings.adminurlusername, My.Settings.adminurlpassword)
            request.Timeout = timeoutms

            'request.Credentials = CredentialCache.DefaultCredentials
            ' Get the response.
            Dim response As WebResponse = request.GetResponse()
            ' Display the status.
            Console.WriteLine(CType(response, HttpWebResponse).StatusDescription)
            ' Get the stream containing content returned by the server.
            Dim dataStream As Stream = response.GetResponseStream()
            ' Open the stream using a StreamReader for easy access.
            Dim reader As New StreamReader(dataStream)
            ' Read the content.
            Dim responseFromServer As String = reader.ReadToEnd()
            ' Display the content.
            Console.WriteLine(responseFromServer)
            ' Clean up the streams and the response.
            reader.Close()
            response.Close()

            Dim content As String = responseFromServer
            Try
                Dim result As JObject = JObject.Parse(content)
                If Not result.Item("fevent") Is Nothing Then
                    Dim r = result.Item("fevent")
                    Dim evtext As String = ""
                    If Not r.Item("title") Is Nothing Then
                        evtext = r.Item("title")
                    End If
                    If Not r.Item("fevent_id") Is Nothing Then
                        evtext += " (" + r.Item("fevent_id").ToString + ")"
                    End If
                    evtext += " / " + My.Settings.actionid
                    textBoxEventTitle.Text = evtext
                    Me.Title = "fluxcess I Was Here * " + evtext
                End If

                Dim roominfo As String = ""
                If (result.Item("err") Is Nothing And Not result.Item("room") Is Nothing) Then
                    'MsgBox(result.Item("room").Item("0").Item("room_id"))
                    'MsgBox(result("room").Count)
                    'MsgBox(result("room").Item(1)("room_id"))
                    Try
                        For Each xz In result("room")
                            roominfo += xz("room_name").ToString + ": " + xz("spax").ToString + ", " '.ToString)
                        Next
                    Catch ex As Exception
                        For Each xz In result("room").First
                            roominfo += xz("room_name").ToString + ": " + xz("spax").ToString + ", " '.ToString)
                        Next
                    End Try
                    'For i = 1 To result.Item("room").Count
                    'MsgBox(result.Item("room")(i.ToString).Item("room_id"))
                    'Next
                End If
                If roominfo.Length > 3 Then
                    roominfo = roominfo.Substring(0, roominfo.Length - 2)
                End If
                'MsgBox(roominfo)
                labelTotalCounter.Content = roominfo
            Catch ex As Exception
                MsgBox("Der Server hat auf die Anfrage an " + url + " nach Eventdetails eine unleserliche Antwort gesendet. " + ex.Message + " " + content)
            End Try

        Catch ex As Exception

        End Try
    End Sub
    Private Sub closePopup(Optional scanner As Boolean = False)
        ccPopup.Visibility = Visibility.Hidden
        popupCloseTimer.Stop()
        isPopupOpen = False
        If scanner = False Then
            If textBoxSearchEx.Text.Length > 0 Then
                If textBoxSearchEx.Text.Substring(0, 1) = "#" Then
                    textBoxSearchEx.Text = ""
                    textBoxSearchEx.Focus()
                Else
                    textBoxSearchEx.SelectAll()
                End If
            End If
        Else
            textBoxSearchEx.CaretIndex = textBoxSearchEx.Text.Length
            textBoxSearchEx.Focus()
        End If
    End Sub

    Private Sub MainWindow_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        'MsgBox(e.Key)

        Select Case e.Key
            Case 145 '
                textBoxSearchEx.Text = "#"
                closePopup(True)
            Case Key.D1
                If (Keyboard.IsKeyDown(Key.LeftCtrl) = True Or Keyboard.IsKeyDown(Key.RightCtrl)) Then
                    ccSettings.Visibility = Visibility.Visible
                End If
            Case Key.Escape
                If isPopupOpen = True Then
                    closePopup()
                Else
                    textBoxSearchEx.Focus()
                End If
            Case Key.Enter
                If isPopupOpen = False Then
                    If textBoxSearchEx.Text.Length > 1 Then

                        labelDefaultMessage.Content = "Code erkannt (" + textBoxSearchEx.Text + ") - lade Daten. Bitte warten."
                        labelDefaultMessage.UpdateLayout()
                        'DoEvents()
                        'DoEvents()

                        'Me.UpdateLayout()
                        'InvalidateVisual()
                        'MsgBox("GetXmlNamespace")

                        If textBoxSearchEx.Text.Length > 2 And textBoxSearchEx.Text.Substring(0, 2) = "#E" And My.Settings.qrcodestyle = "2" Then
                            Dim strparts() As String = textBoxSearchEx.Text.Split("G")
                            If strparts.Length < 2 Then
                                MsgBox("QR Code nicht vollständig (+" + textBoxSearchEx.Text + ")")
                            Else
                                If My.Settings.checkinsearchfieldname.Length > 0 Then
                                    openPopupFromWebcode(My.Settings.checkinsearchfieldname, strparts(1))
                                Else
                                    MsgBox("Check-In Feld nicht definiert.")
                                End If
                            End If
                        ElseIf (textBoxSearchEx.Text.Substring(0, 1) = "#") Then
                            ' qr code style 1
                            openPopupFromWebcode(My.Settings.checkinsearchfieldname, textBoxSearchEx.Text.Substring(1))
                        End If
                        labelDefaultMessage.Content = "Bitte scannen Sie Ihr Ticket!"
                    Else
                        textBoxSearchEx.Text = ""
                        textBoxSearchEx.Focus()
                    End If
                End If
        End Select
    End Sub

    Public Sub DoEvents()
        Dim frame As New DispatcherFrame()
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, New DispatcherOperationCallback(AddressOf ExitFrame), frame)
        Dispatcher.PushFrame(frame)
    End Sub
    Public Function ExitFrame(ByVal f As Object) As Object
        CType(f, DispatcherFrame).Continue = False

        Return Nothing
    End Function



    Private Sub toggleSession(sessionName As String)
        If (Not sessionnames.ContainsKey(sessionName)) Then
            sessionnames.Add(sessionName, True)
        Else
            If (sessionnames.Item(sessionName)) = True Then
                sessionnames.Item(sessionName) = False
            Else
                sessionnames.Item(sessionName) = True
            End If
        End If
    End Sub

    Private Sub updateSessionButton(ByRef myButton As Button, ByVal onoff As Boolean)
        If onoff = False Then
            myButton.Background = Brushes.DarkGray
            myButton.Foreground = Brushes.Black
        Else
            myButton.Background = Brushes.DarkGreen
            myButton.Foreground = Brushes.White
        End If
    End Sub

    Private Sub doGuestCheckinout(inorout As String)
        Try

            'Dim searchexencoded As String = HttpUtility.UrlEncode(searchex, System.Text.Encoding.ASCII)
            'Dim url As String = baseUrl + "/aj/addactionjson"
            Dim url As String = My.Settings.adminurl + "/api/events/" + My.Settings.eventid.ToString + "/guests/" + currentGuestId.ToString + "/checkinout"
            Dim request As WebRequest = WebRequest.Create(url)
            request.Method = "POST"
            SetBasicAuthHeader(request, My.Settings.adminurlusername, My.Settings.adminurlpassword)

            ' Create POST data and convert it to a byte array.

            Dim postData As String = "reqid=5&action=" + My.Settings.applicationmode + "&fevent_id=" + My.Settings.eventid.ToString + "&guest_id=" + currentGuestId.ToString + "&checkinout=" + inorout + "&room_id=" + My.Settings.actionid + "&form=checkinout"

            Dim byteArray As Byte() = Encoding.UTF8.GetBytes(postData)
            request.ContentType = "application/x-www-form-urlencoded"
            request.ContentLength = byteArray.Length
            Dim dataStream As Stream = request.GetRequestStream()
            dataStream.Write(byteArray, 0, byteArray.Length)
            dataStream.Close()
            ' Get the response.
            Dim response As WebResponse = request.GetResponse()
            dataStream = response.GetResponseStream()
            Dim reader As New StreamReader(dataStream)
            Dim content As String = reader.ReadToEnd()
            reader.Close()
            dataStream.Close()
            response.Close()
            Try
                Dim result As JObject = JObject.Parse(content)
                If result.Item("err") Is Nothing Then
                Else
                    MsgBox("Fehler: " + content)
                End If
            Catch ex As Exception
                MsgBox("Fehler beim Parsen der Antwort zum Check-In/Out: " + content.ToString + " * " + ex.Message)
            End Try
        Catch ex As Exception
            MsgBox("y " + ex.Message)
        End Try
    End Sub

    Private Sub addGuestAction()
        Try

            'Dim searchexencoded As String = HttpUtility.UrlEncode(searchex, System.Text.Encoding.ASCII)
            Dim url As String = My.Settings.checkinserver + "/aj/addactionjson"
            'Dim url As String = baseUrl + "/api/events/" + My.Settings.eventid.ToString + "/guests/" + currentGuestId + "/checkinout"
            Dim request As WebRequest = WebRequest.Create(url)
            request.Method = "POST"
            ' Create POST data and convert it to a byte array.

            Dim postData As String = "reqid=5&action=" + My.Settings.actionid + "&fevent_id=" + My.Settings.eventid.ToString + "&id=" + currentGuestId.ToString

            Dim byteArray As Byte() = Encoding.UTF8.GetBytes(postData)
            request.ContentType = "application/x-www-form-urlencoded"
            request.ContentLength = byteArray.Length
            Dim dataStream As Stream = request.GetRequestStream()
            dataStream.Write(byteArray, 0, byteArray.Length)
            dataStream.Close()
            ' Get the response.
            Dim response As WebResponse = request.GetResponse()
            dataStream = response.GetResponseStream()
            Dim reader As New StreamReader(dataStream)
            Dim content As String = reader.ReadToEnd()
            reader.Close()
            dataStream.Close()
            response.Close()
            Try
                Dim result As JObject = JObject.Parse(content)
                If (result.Item("fehler") = "-") Then
                Else
                    MsgBox("Fehler: " + content)
                End If
            Catch ex As Exception
                MsgBox("Fehler beim Parsen der Antwort zum Aktionsprotokoll: " + content.ToString + " * " + ex.Message)
            End Try
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    Private Sub logActionToFile(fieldname As String, fieldvalue As String, guestid As String, guestname As String, actionname As String, actiontitle As String)
        Try
            Dim logmsg As String = Now.ToString + ";" + fieldname + ";" + fieldvalue + ";" + guestid + ";" + guestname.Replace(";", "").Replace(vbNewLine, "") + ";" + actionname.Replace(";", "") + ";" + actiontitle.Replace(";", "")

            Dim dirname As String = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\fluxcess\"
            If Not System.IO.Directory.Exists(dirname) Then
                System.IO.Directory.CreateDirectory(dirname)
            End If
            Dim filename As String = "actionlog_event_" + My.Settings.eventid.ToString + ".txt"

            If Not System.IO.File.Exists(dirname + filename) Then
                Dim objWriter As New System.IO.StreamWriter(dirname + filename, False)
                objWriter.Close()
                logmsg = "time;fieldname;fieldvalue;guestid;guestname;actionname;actiontitle" + vbNewLine + logmsg
            End If
            'MsgBox(logmsg + " " + filename)
            Dim objWriter2 As New System.IO.StreamWriter(dirname + filename, True)
            objWriter2.WriteLine(logmsg)
            objWriter2.Close()

        Catch ex As Exception
            MsgBox("Problem beim Schreiben des Logs: " + ex.Message)
        End Try
    End Sub

    Private Sub buttonSettingsOk_Click(sender As Object, e As RoutedEventArgs) Handles buttonSettingsOk.Click
        saveSettings()
        ccClose()
    End Sub

    Private Sub ccClose()

        ccSettings.Visibility = Visibility.Hidden
    End Sub

    Private Sub saveSettings()
        My.Settings.checkinserver = textBoxServername.Text
        My.Settings.eventid = Convert.ToInt32(textBoxEventId.Text)
        My.Settings.actionid = textBoxRaumId.Text
        If radioButtonOnline.IsChecked = True Then
            My.Settings.onoffline = "online"
        ElseIf radioButtonOnline2.IsChecked = True Then
            My.Settings.onoffline = "online2"
        Else
            My.Settings.onoffline = "offline"
        End If

        If radioButtonQRCodeStyle_2.IsChecked = True Then
            My.Settings.qrcodestyle = 2
        Else
            My.Settings.qrcodestyle = 1
        End If

        If radioButtonModeCheckIn.IsChecked = True Then
            My.Settings.applicationmode = "in"
        ElseIf radioButtonModeCheckOut.IsChecked = True Then
            My.Settings.applicationmode = "out"
        Else
            My.Settings.applicationmode = "act"
        End If

        My.Settings.httpusername = textHttpusername.Text
        My.Settings.httppassword = textHttppassword.Password
        My.Settings.actiontitle = textboxRaumTitle.Text

        My.Settings.adminurl = textBoxAdminUrl.Text
        My.Settings.adminurlusername = textBoxAdminUrlUsername.Text
        My.Settings.adminurlpassword = textBoxAdminUrlPassword.Password

        My.Settings.checkinsearchfieldname = textBoxFieldName.Text

        My.Settings.bgcolor = textboxBackgroundcolor.Text
        My.Settings.bgpic = textboxBackgroundImage.Text
        My.Settings.forecolor = textboxFontcolor.Text

        My.Settings.popupbackgroundcolor = textboxPopupBackgroundcolor.Text
        My.Settings.popupforegroundcolor = textboxPopupFontcolor.Text

        My.Settings.Save()
        designTheApp()
    End Sub

    Private Sub loadSettings()
        textBoxServername.Text = My.Settings.checkinserver
        textBoxEventId.Text = My.Settings.eventid.ToString
        eventIdInitiallyRead = True
        textBoxRaumId.Text = My.Settings.actionid
        If My.Settings.onoffline = "online" Then
            radioButtonOnline.IsChecked = True
        ElseIf My.Settings.onoffline = "online2" Then
            radioButtonOnline2.IsChecked = True
        Else
            radioButtonOffline.IsChecked = True
        End If

        If My.Settings.qrcodestyle = 2 Then
            radioButtonQRCodeStyle_2.IsChecked = True
        Else
            radioButtonQRCodeStyle_1.IsChecked = True
        End If

        If My.Settings.applicationmode = "in" Then
            radioButtonModeCheckIn.IsChecked = True
        ElseIf My.Settings.applicationmode = "out" Then
            radioButtonModeCheckOut.IsChecked = True
        Else
            radioButtonModeSpecial.IsChecked = True
        End If

        textHttpusername.Text = My.Settings.httpusername
        textHttppassword.Password = My.Settings.httppassword
        textboxRaumId.Text = My.Settings.actionid
        textboxRaumTitle.Text = My.Settings.actiontitle

        textBoxAdminUrl.Text = My.Settings.adminurl
        textBoxAdminUrlUsername.Text = My.Settings.adminurlusername
        textBoxAdminUrlPassword.Password = My.Settings.adminurlpassword

        textBoxFieldName.Text = My.Settings.checkinsearchfieldname

        textboxBackgroundcolor.Text = My.Settings.bgcolor
        textboxBackgroundImage.Text = My.Settings.bgpic
        textboxFontcolor.Text = My.Settings.forecolor

        textboxPopupBackgroundcolor.Text = My.Settings.popupbackgroundcolor
        textboxPopupFontcolor.Text = My.Settings.popupforegroundcolor

    End Sub

    Private Sub loadStats()
        Try
            Dim url As String = My.Settings.checkinserver + "/aj/tnzjson"
            ' MsgBox(url)
            Dim request As WebRequest = WebRequest.Create(url)
            request.Method = "POST"
            request.Timeout = timeoutms
            ' Create POST data and convert it to a byte array.
            Dim postData As String = "reqid=5&fevent_id=" + My.Settings.eventid.ToString
            Dim byteArray As Byte() = Encoding.UTF8.GetBytes(postData)
            request.ContentType = "application/x-www-form-urlencoded"
            request.ContentLength = byteArray.Length
            Dim dataStream As Stream = request.GetRequestStream()
            dataStream.Write(byteArray, 0, byteArray.Length)
            dataStream.Close()
            ' Get the response.
            Dim response As WebResponse = request.GetResponse()
            dataStream = response.GetResponseStream()
            Dim reader As New StreamReader(dataStream)
            Dim content As String = reader.ReadToEnd()
            reader.Close()
            dataStream.Close()
            response.Close()
            Try
                Dim result As JObject = JObject.Parse(content)
                If (result.Item("fehler") = "-") Then
                    Dim anz As Integer = 0
                    If Not result.Item("inhalt").Item("fevent") Is Nothing Then
                        Dim r = result.Item("inhalt").Item("fevent")
                        Dim evtext As String = ""
                        If Not r.Item("title") Is Nothing Then
                            evtext = r.Item("title")
                        End If
                        If Not r.Item("fevent_id") Is Nothing Then
                            evtext += " (" + r.Item("fevent_id").ToString + ")"
                        End If
                        evtext += " / " + My.Settings.actionid
                        textBoxEventTitle.Text = evtext
                        Me.Title = "fluxcess I Was Here * " + evtext
                    Else
                        MsgBox(content)
                    End If
                Else
                    MsgBox(content)
                End If
            Catch ex As Exception
                MsgBox("Problem beim Parsen der Statistik. " + content.ToString + " " + ex.Message)
            End Try

        Catch ex As Exception
        End Try
    End Sub
    Private Sub timer_Tick(sender As Object, e As EventArgs) Handles dt.Tick
        If My.Settings.onoffline = "online" Then
            loadStats()
        ElseIf My.Settings.onoffline = "online2" Then
            loadStats2()
        End If
    End Sub

    Public Sub SetBasicAuthHeader(ByRef mrequest As WebRequest, userName As String, userPassword As String)

        Dim authInfo As String = userName + ":" + userPassword
        authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo))
        mrequest.Headers("Authorization") = "Basic " + authInfo
    End Sub

    Private Sub loadEventData()
        Dim url As String = ""
        Dim content As String = ""
        Dim fromFile As Boolean = False
        Try
            url = My.Settings.adminurl + "/api/events/" + My.Settings.eventid.ToString
            Try
                Dim myClient = New WebClient()

                myClient.UseDefaultCredentials = True
                myClient.Credentials = New NetworkCredential(My.Settings.adminurlusername, My.Settings.adminurlpassword)
                content = myClient.DownloadString(url)

            Catch ex As Exception
                content = readJsonEventdataFromFile(My.Settings.eventid)
                fromFile = True
            End Try

            Dim result As JObject = Nothing
            Try
                result = JObject.Parse(content)
                If fromFile = False Then
                    storeJsonEventData(My.Settings.eventid, content)
                End If
            Catch ex As Exception
                fluxcessLog.logMsg("Problem parsing the event JSON: " + ex.Message + " | content: " + content, 5)
            End Try
            If Not result Is Nothing Then
                If result.Item("err") Is Nothing And Not result.Item("event") Is Nothing Then
                    Dim rec = result.Item("event")
                    fluxcessLog.logMsg("Loaded event data for event id #" + My.Settings.eventid.ToString + " from server.", 20)
                    eventSettings = New eventSettings
                    eventSettings.loadSettings(rec)
                    'fluxcessLog.logMsg("Event data: " + rec.ToString, 50)
                    ' ToDo: Parse it - or at least store it.
                    'adaptCheckinForm(rec)
                    'MsgBox("Got the data: " + content)

                    readRoomSets(rec)
                Else
                    fluxcessLog.logMsg("Problem reading the event data. Server answer is: " + content, 8)
                    MsgBox("Problem reading event data. Content: " + content)
                End If
            End If
        Catch ex As Exception
            fluxcessLog.logMsg("Problem loading event data (" + url + ") : " + ex.Message, 5)
        End Try
    End Sub
    Private Sub loadEventFields()
        Dim url As String = ""
        Dim content As String = ""
        Dim fromFile As Boolean = False
        Try
            url = My.Settings.adminurl + "/api/events/" + My.Settings.eventid.ToString + "/fields"
            Try
                Dim myClient = New WebClient()

                myClient.UseDefaultCredentials = True
                myClient.Credentials = New NetworkCredential(My.Settings.adminurlusername, My.Settings.adminurlpassword)
                content = myClient.DownloadString(url)

            Catch ex As Exception
                content = readJsonFielddataFromFile(My.Settings.eventid)
                fromFile = True
            End Try

            Dim result As JObject = Nothing
            Try
                result = JObject.Parse(content)
                If fromFile = False Then
                    storeJsonFieldData(My.Settings.eventid, content)
                End If
            Catch ex As Exception
                fluxcessLog.logMsg("Problem parsing the event fields JSON: " + ex.Message + " | content: " + content, 5)
            End Try


            If Not result Is Nothing Then
                If result.Item("err") Is Nothing And Not result.Item("meta") Is Nothing Then
                    fluxcessLog.logMsg("Loaded event field data for event id #" + My.Settings.eventid.ToString + " from server.", 20)
                    readDataFields(result)
                Else
                    fluxcessLog.logMsg("Problem reading the event field data. Server answer is: " + content, 8)
                    MsgBox("Problem reading event field data. Content: " + content)
                End If
            End If
        Catch ex As Exception
            fluxcessLog.logMsg("Problem loading event field data (" + url + ") : " + ex.Message, 5)
        End Try
    End Sub
    Private Sub loadEvents()
        Dim url As String = ""
        Dim content As String = ""
        Dim fromFile As Boolean = False
        Try
            url = My.Settings.adminurl + "/api/events/"
            Try
                Dim myClient = New WebClient()

                myClient.UseDefaultCredentials = True
                myClient.Credentials = New NetworkCredential(My.Settings.adminurlusername, My.Settings.adminurlpassword)
                content = myClient.DownloadString(url)

            Catch ex As Exception
                content = readJsonEventsFromFile()
                fromFile = True
            End Try

            Dim result As JObject = Nothing
            Try
                result = JObject.Parse(content)
                If fromFile = False Then
                    storeJsonEvents(content)
                End If
            Catch ex As Exception
                fluxcessLog.logMsg("Problem parsing the event list JSON: " + ex.Message + " | content: " + content, 5)
            End Try


            If Not result Is Nothing Then
                If result.Item("err") Is Nothing And Not result.Item("events") Is Nothing Then
                    fluxcessLog.logMsg("Loaded event list from server.", 20)
                    Dim rec As JArray = result.Item("events")
                    readEvents(rec)
                Else
                    fluxcessLog.logMsg("Problem reading the event list. Server answer is: " + content, 8)
                    MsgBox("Problem reading event list. Content: " + content)
                End If
            End If
        Catch ex As Exception
            fluxcessLog.logMsg("Problem loading event list (" + url + ") : " + ex.Message, 5)
        End Try
    End Sub

    Private Sub storeJsonEventData(eventid As Integer, data As String)
        If data.Length > 8 Then
            Dim storepath As String = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)
        If storepath.Substring(storepath.Length - 1, 1) <> "\" Then
            storepath += "\"
        End If
        storepath += "fluxcess_GmbH\fluxcessIWasHere\cache\"
            If Not System.IO.Directory.Exists(storepath) Then
                MkDir(storepath)
            End If
            Dim filename As String = storepath + "event_" + eventid.ToString + ".json"
            File.Delete(filename)
            Dim w As StreamWriter = File.AppendText(filename)
            w.Write(data)
            w.Close()
        End If
    End Sub
    Private Sub storeJsonFieldData(eventid As Integer, data As String)
        If data.Length > 8 Then
            Dim storepath As String = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)
            If storepath.Substring(storepath.Length - 1, 1) <> "\" Then
                storepath += "\"
            End If
            storepath += "fluxcess_GmbH\fluxcessIWasHere\cache\"
            If Not System.IO.Directory.Exists(storepath) Then
                MkDir(storepath)
            End If
            Dim filename As String = storepath + "event_" + eventid.ToString + "_fields.json"
            File.Delete(filename)
            Dim w As StreamWriter = File.AppendText(filename)
            w.Write(data)
            w.Close()
        End If
    End Sub
    Private Sub storeJsonEvents(data As String)
        If data.Length > 8 Then
            Dim storepath As String = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)
            If storepath.Substring(storepath.Length - 1, 1) <> "\" Then
                storepath += "\"
            End If
            storepath += "fluxcess_GmbH\fluxcessIWasHere\cache\"
            If Not System.IO.Directory.Exists(storepath) Then
                MkDir(storepath)
            End If
            Dim filename As String = storepath + "events.json"
            File.Delete(filename)
            Dim w As StreamWriter = File.AppendText(filename)
            w.Write(data)
            w.Close()
        End If
    End Sub
    Private Function readJsonEventdataFromFile(eventid As Integer) As String
        Dim content As String = ""
        Try

            Dim storepath As String = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)
            If storepath.Substring(storepath.Length - 1, 1) <> "\" Then
                storepath += "\"
            End If
            storepath += "fluxcess_GmbH\fluxcessIWasHere\cache\"
            If Not System.IO.Directory.Exists(storepath) Then
                MkDir(storepath)
            End If

            content = File.ReadAllText(storepath + "event_" + eventid.ToString + ".json")
        Catch ex As Exception
            fluxcessLog.logMsg("Einlesen der Eventdaten nicht möglich: " + ex.Message, 1)
        End Try
        Return content
    End Function
    Private Function readJsonFielddataFromFile(eventid As Integer) As String
        Dim content As String = ""
        Try

            Dim storepath As String = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)
            If storepath.Substring(storepath.Length - 1, 1) <> "\" Then
                storepath += "\"
            End If
            storepath += "fluxcess_GmbH\fluxcessIWasHere\cache\"
            If Not System.IO.Directory.Exists(storepath) Then
                MkDir(storepath)
            End If

            content = File.ReadAllText(storepath + "event_" + eventid.ToString + "_fields.json")
        Catch ex As Exception
            fluxcessLog.logMsg("Einlesen der Eventdaten nicht möglich: " + ex.Message, 1)
        End Try
        Return content
    End Function
    Private Function readJsonEventsFromFile() As String
        Dim content As String = ""
        Try

            Dim storepath As String = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)
            If storepath.Substring(storepath.Length - 1, 1) <> "\" Then
                storepath += "\"
            End If
            storepath += "fluxcess_GmbH\fluxcessIWasHere\cache\"
            If Not System.IO.Directory.Exists(storepath) Then
                MkDir(storepath)
            End If

            content = File.ReadAllText(storepath + "events.json")
        Catch ex As Exception
            fluxcessLog.logMsg("Einlesen der Eventliste nicht möglich: " + ex.Message, 1)
        End Try
        Return content
    End Function

    Private Sub readRoomSets(data As JObject)
        Try
            roomSets = New Dictionary(Of String, String)
            roomsInRoomSets = New Dictionary(Of String, Dictionary(Of String, String))

            Dim roomsetsTMP As Dictionary(Of Integer, Dictionary(Of String, String))
            roomsetsTMP = eventSettings.getComplexSettingBySeq("roomset")

            Dim roomsTmp As New Dictionary(Of Integer, Dictionary(Of String, String))
            roomsTmp = eventSettings.getComplexSettingBySeq("room")

            comboBoxRooms.Items.Clear()
            allRoomIds = New Dictionary(Of Integer, String)
            allRoomTitles = New Dictionary(Of Integer, String)

            Dim myRoomCounter As Integer = 0
            For Each kvp As KeyValuePair(Of Integer, Dictionary(Of String, String)) In roomsTmp
                myRoomCounter += 1
                allRoomIds.Add(myRoomCounter, kvp.Value.Item("id"))
                allRoomTitles.Add(myRoomCounter, kvp.Value.Item("title"))
                comboBoxRooms.Items.Insert(myRoomCounter - 1, kvp.Value.Item("title"))

            Next
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub



    Private Sub readDataFields(data As JObject)
        Try
            comboBoxFields.Items.Clear()
            allDataFields = New Dictionary(Of Integer, String)
            Dim fieldCounter As Integer = 0
            For Each row In data
                If row.Key.ToString <> "meta" Then
                    fieldCounter += 1
                    'MsgBox(row.Value.Item("fieldname"))
                    allDataFields.Add(fieldCounter, row.Value.Item("fieldname"))
                    comboBoxFields.Items.Insert(fieldCounter - 1, row.Value.Item("fieldname"))
                End If
            Next
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    Private Sub readEvents(data As JArray)
        Try
            comboBoxEvents.Items.Clear()
            allEventIds = New Dictionary(Of Integer, String)
            allEventNames = New Dictionary(Of Integer, String)

            Dim fieldCounter As Integer = 0
            For Each row In data
                fieldCounter += 1
                allEventIds.Add(fieldCounter, row.Item("id"))
                allEventNames.Add(fieldCounter, row.Item("title"))
                comboBoxEvents.Items.Insert(fieldCounter - 1, row.Item("title").ToString + " (" + row.Item("id").ToString + ")")
            Next
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub



    Private Sub button_Click(sender As Object, e As RoutedEventArgs) Handles button.Click
        loadEventData()
    End Sub

    Private Sub comboBoxRooms_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles comboBoxRooms.SelectionChanged
        textBoxRaumId.Text = allRoomIds.Item(comboBoxRooms.SelectedIndex + 1)
        textboxRaumTitle.Text = allRoomTitles.Item(comboBoxRooms.SelectedIndex + 1)
    End Sub

    Private Sub buttonReloadFieldList_Click(sender As Object, e As RoutedEventArgs) Handles buttonReloadFieldList.Click
        loadEventFields()
    End Sub

    Private Sub buttonReloadFieldList_Copy_Click(sender As Object, e As RoutedEventArgs) Handles buttonReloadFieldList_Copy.Click
        loadEvents()
    End Sub

    Private Sub comboBoxEvents_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles comboBoxEvents.SelectionChanged
        textBoxEventId.Text = allEventIds.Item(comboBoxEvents.SelectedIndex + 1)
    End Sub

    Private Sub comboBoxFields_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles comboBoxFields.SelectionChanged
        textBoxFieldName.Text = comboBoxFields.SelectedValue
    End Sub

    Private Sub textBoxEventId_TextChanged(sender As Object, e As TextChangedEventArgs) Handles textBoxEventId.TextChanged
        If eventIdInitiallyRead = True Then
            My.Settings.eventid = Convert.ToInt32(textBoxEventId.Text)
            My.Settings.Save()
        End If
    End Sub
End Class
