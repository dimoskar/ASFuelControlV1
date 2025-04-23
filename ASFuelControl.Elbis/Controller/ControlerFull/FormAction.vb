
Public Class frmAction

    ' Declare as Private With Events Because Used ONLY on This Form.
    ' Is Impossible to Declare Public With Events on a Module.
    Private WithEvents MyComm As New ElbisDLL_New.clsComm               ' Declare the Communication Class on ElbisMyDLL Library.

    Dim AskAction As Integer                                            ' Indicate The Ask Action on Automatic Working.
    Dim SettingsAsk As Boolean                                          ' Indicate if we Ask About Pressetings on Controler.
    Dim AutoActionWait As Integer = 0                                   ' Used On Reset Controler to Has Time the Controler for Initialize.

    Dim WorkingWithUSB_Nr As Long = -1                                  ' This is the Selected USB Number for Working.

    Dim TimeStart As Date                                               ' Helping for Console Action Duration.
    Dim WorkWithChannel As Integer                                      ' Number of Channel Where We Working Now. 01 to Max.Channels
    Dim TransparentLength As Long                                       ' Outgoing Transparent Packet Length.

    Dim StatusEventTime As Long = 1
    Dim PressetsEventTime As Long = 1
    Dim DeliveryEventTime As Long = 1
    Dim TotalsEventTime As Long = 1

    Private Sub frmAction_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load

        cmbDeviceModel.Items.Clear()                                    ' Preparate Pomp Model Combo.
        For MetMe = 0 To UBound(MyComm.DeviceModel)
            cmbDeviceModel.Items.Add(MyComm.DeviceModel(MetMe))
        Next

        cmbCommPort.Items.Clear()                                       ' Preparate Communication Port Combo. (0-7)
        For MetMe = 0 To UBound(MyComm.CommPort)
            cmbCommPort.Items.Add(MyComm.CommPort(MetMe))
        Next

        cmbCommStyle.Items.Clear()                                      ' Preparate Communication Style Combo. (RS485,RS422)
        For MetMe = 0 To UBound(MyComm.CommStyle)
            cmbCommStyle.Items.Add(MyComm.CommStyle(MetMe))
        Next

        cmbCommSpeed.Items.Clear()                                      ' Preparate Communication Speed Combo. ("Auto", "1200", "2400", "4800", "5787", "9600", "19200")
        For MetMe = 0 To UBound(MyComm.CommSpeed)
            cmbCommSpeed.Items.Add(MyComm.CommSpeed(MetMe))
        Next

        cmbCommDatas.Items.Clear()                                      ' Preparate Communication Datas Combo. ("Auto", "8,n,1", "8,e,1", "8,o,1", "7,n,1", "7,e,1", "7,o,1", "8,n,2", "8,e,2", "8,o,2", "7,n,2", "7,e,2", "7,o,2")
        For MetMe = 0 To UBound(MyComm.CommDatas)
            cmbCommDatas.Items.Add(MyComm.CommDatas(MetMe))
        Next

        cmbProducts.Items.Clear()                                       ' Preparate Products Combo.
        For MetMe = 0 To UBound(MyComm.Products)
            cmbProducts.Items.Add(MyComm.Products(MetMe))
        Next

        ToolTip1.ToolTipTitle = "HTA Informations."
        ToolTip1.UseFading = True
        ToolTip1.UseAnimation = True
        ToolTip1.IsBalloon = True
        ToolTip1.ShowAlways = True
        ToolTip1.AutoPopDelay = 5000
        ToolTip1.InitialDelay = 1000
        ToolTip1.ReshowDelay = 500
        ToolTip1.IsBalloon = True
        ToolTip1.ToolTipIcon = ToolTipIcon.Info
        ToolTip1.BackColor = Color.Yellow

        Me.ToolTip1.SetToolTip(Me.cmdAutoRun, "Ask All Channels About All Delivery Data.")

        With gridAction
            .Rows.Clear()
            .Columns("Chan").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("NzNz").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("Points").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("PStat").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("PErr").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

            .Columns("APStat").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("Master").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("Alive").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("Block").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("Auth").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("GetTra").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("NeedA").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("OnFill").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("NzOut").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("DPrice").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            .Columns("DAmou").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            .Columns("DVolu").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            .Columns("Price").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            .Columns("CAmou").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            .Columns("CVolu").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            .Columns("TAmou").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            .Columns("TVolu").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

            .Columns("Pomp").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            .Columns("Iden").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("Nozz").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("Port").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("Styl").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("Sped").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("Data").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            .Columns("Prod").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        End With

        For MetA = 1 To UBound(MyComm.Channel)                                      ' Preparate Data Grid View.
            gridAction.Rows.Add()
            gridAction.Item(0, MetA - 1).Value = MetA
        Next

        WorkingWithUSB_Nr = -1                                                                              ' Device Number => ERROR

        gridUSB_Scan.Rows.Clear()

        Dim TotalDevices As Integer                                                                         ' Hold Total FTDI Devices Found on Our System.
        Call MyComm.ScanAllDevices(TotalDevices)                                                            ' Check For FTDI USB Total Devices.

        If TotalDevices > 0 Then                                                                            ' If Found Devices
            For MetA = 0 To TotalDevices - 1                                                                ' For All Pressent Devices
                gridUSB_Scan.Rows.Add()
                gridUSB_Scan.Item(0, MetA).Value = MetA + 1
                gridUSB_Scan.Item("Description", MetA).Value = MyComm.FTDI_Device(MetA + 1).Description     ' Get FTDI USB Device Description
                gridUSB_Scan.Item("Serial_Number", MetA).Value = MyComm.FTDI_Device(MetA + 1).SerialNumber  ' Get FTDI USB Device Serial Number
            Next
        End If

        Call VisiblesAtScreen(True)

    End Sub

    Private Sub gridUSB_Scan_CellMouseDoubleClick(sender As Object, e As System.Windows.Forms.DataGridViewCellMouseEventArgs) Handles gridUSB_Scan.CellMouseDoubleClick

        Try
            WorkingWithUSB_Nr = gridUSB_Scan.Item(0, e.RowIndex).Value - 1
            MyComm.USB_WorkDeviceNr = WorkingWithUSB_Nr                         ' Update Class Communication About Working Device Number
            If WorkingWithUSB_Nr > -1 Then
                Call UpdateAction()
                Call VisiblesAtScreen()
                gridUSB_Scan.Visible = False
            End If
        Catch ex As Exception
            Beep()
            Exit Sub
        End Try

    End Sub

    Public Sub UpdateAction()

        WorkWithChannel = 1                                                         ' Default Working Channel = 1.
        lblWorkWithChannel.Text = WorkWithChannel                                   ' Update Label.

        System.Windows.Forms.Application.DoEvents()

        If WorkingWithUSB_Nr < 0 Then
            MsgBox("There is NOT Selected USB Device." & vbCrLf & "Please Select from 'Select USB' menu.", MsgBoxStyle.OkOnly, "Error USB Device Nr.")
        Else
            If MyComm.InitDeviceNew() = True Then                                   ' Initialize USB Device.
                txtExplane.Text = "USB Initialized Correct...."
            End If

            cmdAutoRun.Text = "RUN"
            cmdAutoRun.Visible = True
            SettingsAsk = False                                                     ' We don't Ask Pressetings Yet. ATTENTION : First Must be Ask Pressetings.
            tmrExtra.Enabled = True                                                 ' This Timer Working for Explane and Control the Transparent Packet.
        End If

    End Sub

    ' Change Unit Price on a Channel.
    Private Sub cmdChangePrice_Click(sender As System.Object, e As System.EventArgs) Handles cmdChangePrice.Click
        MyComm.ChangeUnitPrice(txtChangePrice.Text, WorkWithChannel)
    End Sub

    ' Preset Volume on a Channel.
    Private Sub cmdPresetVolume_Click(sender As System.Object, e As System.EventArgs) Handles cmdPresetVolume.Click
        MyComm.MakePresetVolume(txtPresetVolume.Text, WorkWithChannel)
    End Sub

    ' Preset Amount on a Channel.
    Private Sub cmdPresetAmount_Click(sender As System.Object, e As System.EventArgs) Handles cmdPresetAmount.Click
        MyComm.MakePresetAmount(txtPresetAmount.Text, WorkWithChannel)
    End Sub

    Private Sub cmdClearList_Click(sender As System.Object, e As System.EventArgs) Handles cmdClearList.Click
        Call ClearDataGridView()
        Call MyComm.RefreshAllData()
    End Sub

    Private Sub ClearDataGridView()
        For MetA = 0 To UBound(MyComm.Channel) - 1                                                      ' Clear All Data on Action Grid
            For MetB = 1 To gridAction.ColumnCount - 1
                gridAction.Item(MetB, MetA).Value = " "
                gridAction.Item(MetB, MetA).Style.BackColor = Color.White
            Next
        Next
    End Sub

    Private Sub txtActionTimes_DoubleClick(sender As Object, e As System.EventArgs) Handles txtActionTimes.DoubleClick
        MyComm.Communication.TimesErrors = 0
        MyComm.Communication.TimesAction = 0                                    ' Clear Error Times And Action Times
        TimeStart = DateTime.Now
    End Sub

    Private Sub tmrAutoRun_Tick(sender As System.Object, e As System.EventArgs) Handles tmrAutoRun.Tick

        ' Used On RESTART Controler Action for Wait Until Controler Initialized Correct.
        If AutoActionWait > 0 Then
            AutoActionWait = AutoActionWait - 1
            txtExplane.Text = "  Please Wait Controller to Refresh. " & AutoActionWait.ToString
            txtExplane.BackColor = IIf(AutoActionWait Mod 2, Color.GreenYellow, Color.White)
            If AutoActionWait = 0 Then
                AskAction = 0
            End If
            Exit Sub
        End If

        lblRunInfo.Text = AskAction

        Select Case AskAction
            Case ElbisDLL_New.clsComm.DD_ASK_PRESSETINGS
                Call MyComm.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_PRESSETINGS)                      ' Read Team A
            Case ElbisDLL_New.clsComm.DD_ASK_DELIVERIES
                Call MyComm.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_DELIVERIES)                       ' Read Team B
            Case ElbisDLL_New.clsComm.DD_ASK_TOTALS
                Call MyComm.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_TOTALS)                           ' Read Team C
            Case ElbisDLL_New.clsComm.DD_SEND_RESTART_CONTROLLER
                Call ClearDataGridView()
                Call MyComm.ReadController_DD(ElbisDLL_New.clsComm.DD_SEND_RESTART_CONTROLLER)              ' Reset Controler.
                AutoActionWait = 25                                                                         ' Set Wait Time to Establised Controler.
            Case Else
                Stop
        End Select

        AskAction = AskAction + 1                                                                           ' Set Next Round Action
        If AskAction > 2 Then
            AskAction = 0
        End If

        Dim dFrom As DateTime
        Dim dTo As DateTime
        If DateTime.TryParse(TimeStart, dFrom) AndAlso DateTime.TryParse(DateTime.Now, dTo) Then
            Dim TS As TimeSpan = dTo - dFrom
            Dim hour As Integer = TS.Hours
            Dim mins As Integer = TS.Minutes
            Dim secs As Integer = TS.Seconds
            txtElapseSeconds.Text = ((hour.ToString("00") & ":") + mins.ToString("00") & ":") + secs.ToString("00")
        End If

        txtActionTimes.Text = MyComm.Communication.TimesErrors & "/" & MyComm.Communication.TimesAction     ' Update Screen About Communication Errors/Action Times.

    End Sub

    Private Sub btnAskSettings_Click(sender As System.Object, e As System.EventArgs) Handles btnAskSettings.Click
        Call MyComm.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_PRESSETINGS)
    End Sub

    Private Sub btnAskDeliveries_Click(sender As System.Object, e As System.EventArgs) Handles btnAskDeliveries.Click
        Call MyComm.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_DELIVERIES)
    End Sub

    Private Sub btnGetTotals_Click(sender As System.Object, e As System.EventArgs) Handles btnGetTotals.Click
        Call MyComm.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_TOTALS)
    End Sub

    Private Sub Button1_Click(sender As System.Object, e As System.EventArgs) Handles btnAskSettingsUsed.Click
        Call MyComm.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_PRESSETINGS_USED)
    End Sub

    Private Sub Button2_Click(sender As System.Object, e As System.EventArgs) Handles btnAskDeliveriesUsed.Click
        Call MyComm.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_DELIVERIES_USED)
    End Sub

    Private Sub Button3_Click(sender As System.Object, e As System.EventArgs) Handles btnGetTotalsUsed.Click
        Call MyComm.ReadController_DD(ElbisDLL_New.clsComm.DD_ASK_TOTALS_USED)
    End Sub

    ' Controler RESTART
    Private Sub btnRestart_Click(sender As System.Object, e As System.EventArgs) Handles btnRestart.Click
        If tmrAutoRun.Enabled Then
            Call MyComm.RefreshAllData()
            AskAction = ElbisDLL_New.clsComm.DD_SEND_RESTART_CONTROLLER
        Else
            Call MyComm.ReadController_DD(ElbisDLL_New.clsComm.DD_SEND_RESTART_CONTROLLER)
        End If
    End Sub

    Private Function RemoveFront(FromString As String, RemoveBytes As Long) As String
        RemoveFront = Mid(FromString, RemoveBytes + 1, Len(FromString) - RemoveBytes)
    End Function

    ' Store Pressetings on a Channel.
    Private Sub cmdStoreSets_Click(sender As System.Object, e As System.EventArgs) Handles cmdStoreSets.Click

        If WorkWithChannel = 0 Then
            Beep()
        Else
            Dim HerePompModel As Long = CLng(cmbDeviceModel.SelectedIndex)                                              ' Topic Variable.
            MyComm.CommData(8) = Chr(cmbDeviceModel.SelectedIndex)                                                      ' Device Model

            If MyComm.DeviceModel(HerePompModel) = "Not Used" Then
                MyComm.CommData(9) = Chr(0)
                MyComm.CommData(10) = Chr(0)
                MyComm.CommData(11) = Chr(0)
                MyComm.CommData(12) = Chr(0)
                MyComm.CommData(13) = Chr(0)
                MyComm.CommData(14) = Chr(0)
                MyComm.CommData(15) = Chr(0)
                MyComm.CommData(16) = Chr(&H10)                                                                         ' Set 1 Nozzle For Make a Round.

            Else
                If MyComm.DeviceModel(HerePompModel) = "Italiana" Then
                    txtIdentity.Text = "000000" & txtIdentity.Text
                    txtIdentity.Text = Mid(txtIdentity.Text, Len(txtIdentity.Text) - 5)
                    MyComm.CommData(9) = Chr("&H" & Mid(txtIdentity.Text, 1, 2))
                    MyComm.CommData(10) = Chr("&H" & Mid(txtIdentity.Text, 3, 2))
                    MyComm.CommData(11) = Chr("&H" & Mid(txtIdentity.Text, 5, 2))                                       ' UseIdentity 3 Bytes
                    txtIdentity.Text = CLng(txtIdentity.Text)

                ElseIf MyComm.DeviceModel(HerePompModel) = "AK" Then
                    txtIdentity.Text = "000000" & txtIdentity.Text
                    txtIdentity.Text = Mid(txtIdentity.Text, Len(txtIdentity.Text) - 5)
                    MyComm.CommData(9) = Chr("&H" & Mid(txtIdentity.Text, 1, 2))
                    MyComm.CommData(10) = Chr("&H" & Mid(txtIdentity.Text, 3, 2))
                    MyComm.CommData(11) = Chr("&H" & Mid(txtIdentity.Text, 5, 2))                                       ' UseIdentity 3 Bytes
                    txtIdentity.Text = CLng(txtIdentity.Text)

                Else
                    If MyComm.Channel(WorkWithChannel).ActionPoint.UseNozzle < 2 Then
                        MyComm.CommData(9) = Chr(CLng(cmbTotalNozzles.SelectedItem) * &H10)                             ' Total Nozzle 1-6.
                    Else
                        MyComm.CommData(9) = Chr(&H90)                                                                  ' $80 = Update Only This Channel.(Not Set UsedNozzle)
                    End If
                    MyComm.CommData(10) = Chr(0)
                    MyComm.CommData(11) = Chr(CLng(cmbIdentity.SelectedIndex))                                          ' UseIdentity 1 Byte
                End If

                MyComm.CommData(12) = Chr(CLng(cmbCommPort.SelectedIndex))                                              ' UseNozzle & Port. (UseNozzle Updated from Controler)
                MyComm.CommData(13) = Chr(CLng((cmbCommStyle.SelectedIndex) * &H10) + CLng(cmbProducts.SelectedIndex))  ' CommStyle & Product
                MyComm.CommData(14) = Chr(CLng(cmbCommSpeed.SelectedIndex))                                             ' CommSpeed
                MyComm.CommData(15) = Chr(CLng(cmbCommDatas.SelectedIndex))                                             ' CommDatas
                MyComm.CommData(16) = Chr(CLng(cmbTotalNozzles.SelectedItem) * &H10)                                    ' Total Nozzles. (High Nibble)

            End If

            Call MyComm.WriteController_SS(CLng(WorkWithChannel) - 1)
        End If

    End Sub

    '   ---- On Start/Stop Autorun Action. ----
    Private Sub cmdAutoRun_Click(sender As System.Object, e As System.EventArgs) Handles cmdAutoRun.Click
        Call MyComm.RefreshAllData()
        tmrAutoRun.Enabled = Not tmrAutoRun.Enabled                                         ' Swap Auto Run Timer Enabled
        If tmrAutoRun.Enabled = True Then
            Call MyComm.RefreshAllData()
        End If
        Call VisiblesAtScreen()
    End Sub

    Private Sub gridAction_CellContentDoubleClick(sender As Object, e As System.Windows.Forms.DataGridViewCellEventArgs) Handles gridAction.CellContentDoubleClick
        If gridAction.Columns(e.ColumnIndex).Name = "GetTra" Then
            If gridAction.Item(e.ColumnIndex, e.RowIndex).Value = "YES" Then
                MyComm.Communication.ChannelAction(e.RowIndex + 1) = ElbisDLL_New.clsComm.CHANNEL_CLEAR_GET_TRANSACTION_FLAG    ' Clear Get Transaction Flag.
            End If
        ElseIf gridAction.Columns(e.ColumnIndex).Name = "PErr" Then
            If gridAction.Item(e.ColumnIndex, e.RowIndex).Value <> "0" Then
                MyComm.Communication.ChannelAction(e.RowIndex + 1) = ElbisDLL_New.clsComm.CHANNEL_CLEAR_LAST_PUMP_ERROR         ' Clear Last Error On Pump.
            End If
        End If
    End Sub

    Private Sub btnBlock_Click(sender As System.Object, e As System.EventArgs) Handles btnBlock.Click
        MyComm.Communication.ChannelAction(CInt(txtBlock.Text)) = ElbisDLL_New.clsComm.CHANNEL_BLOCK
    End Sub

    Private Sub btnResume_Click(sender As System.Object, e As System.EventArgs) Handles btnResume.Click
        MyComm.Communication.ChannelAction(CInt(txtBlock.Text)) = ElbisDLL_New.clsComm.CHANNEL_RESUME
    End Sub

    ' Set ToolTip on Cells of Data Grid View.
    Private Sub gridAction_CellFormatting(sender As Object, e As System.Windows.Forms.DataGridViewCellFormattingEventArgs) Handles gridAction.CellFormatting

        If e.ColumnIndex = Me.gridAction.Columns("NzNz").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Total Nozzles / Last Nozzle Up at Action Point : " & e.RowIndex + 1.ToString
        ElseIf e.ColumnIndex = Me.gridAction.Columns("Points").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Decimals at Unit Price, Amount and Volume at Action Point : " & e.RowIndex + 1.ToString
        ElseIf e.ColumnIndex = Me.gridAction.Columns("PStat").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Original Pump Status at Action Point : " & e.RowIndex + 1.ToString
        ElseIf e.ColumnIndex = Me.gridAction.Columns("PErr").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Responsed Last Error from Pump at Action Point : " & e.RowIndex + 1.ToString

        ElseIf e.ColumnIndex = Me.gridAction.Columns("APStat").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Action Point Status of Channel : " & e.RowIndex + 1.ToString
        ElseIf e.ColumnIndex = Me.gridAction.Columns("Master").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Indicate if Channel : " & e.RowIndex + 1.ToString & " is Master."
        ElseIf e.ColumnIndex = Me.gridAction.Columns("Alive").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Indicate if Channel : " & e.RowIndex + 1.ToString & " is Alive."
        ElseIf e.ColumnIndex = Me.gridAction.Columns("Block").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Indicate if Channel : " & e.RowIndex + 1.ToString & " is Blocked."
        ElseIf e.ColumnIndex = Me.gridAction.Columns("Auth").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Indicate if Channel : " & e.RowIndex + 1.ToString & " Authorized."
        ElseIf e.ColumnIndex = Me.gridAction.Columns("GetTra").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Indicate if Channel : " & e.RowIndex + 1.ToString & " Finish Transaction and Collect Data."
        ElseIf e.ColumnIndex = Me.gridAction.Columns("NeedA").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Indicate if Channel : " & e.RowIndex + 1.ToString & " Need Authorize."
        ElseIf e.ColumnIndex = Me.gridAction.Columns("OnFill").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Indicate if Channel : " & e.RowIndex + 1.ToString & " on Filling Now."
        ElseIf e.ColumnIndex = Me.gridAction.Columns("NzOut").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Indicate if Channel : " & e.RowIndex + 1.ToString & " is Out of Hook."
        ElseIf e.ColumnIndex = Me.gridAction.Columns("DPrice").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Display Price of Channel : " & e.RowIndex + 1.ToString
        ElseIf e.ColumnIndex = Me.gridAction.Columns("DAmou").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Display Amount of Channel : " & e.RowIndex + 1.ToString
        ElseIf e.ColumnIndex = Me.gridAction.Columns("DVolu").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Display Volume of Channel : " & e.RowIndex + 1.ToString
        ElseIf e.ColumnIndex = Me.gridAction.Columns("Price").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Unit Price of Channel : " & e.RowIndex + 1.ToString
        ElseIf e.ColumnIndex = Me.gridAction.Columns("CAmou").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Current Amount of Channel : " & e.RowIndex + 1.ToString
        ElseIf e.ColumnIndex = Me.gridAction.Columns("CVolu").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Current Volume of Channel : " & e.RowIndex + 1.ToString
        ElseIf e.ColumnIndex = Me.gridAction.Columns("TVolu").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Total Volume of Channel : " & e.RowIndex + 1.ToString
        ElseIf e.ColumnIndex = Me.gridAction.Columns("TAmou").Index AndAlso (e.Value IsNot Nothing) Then
            Me.gridAction.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = "Total Amount of Channel : " & e.RowIndex + 1.ToString
        End If
    End Sub

    '   ---- On Mouse Click on a Cells of Data Grid View. => Check and Explane Settings. ----
    Private Sub gridAction_CellMouseClick(sender As Object, e As System.Windows.Forms.DataGridViewCellMouseEventArgs) Handles gridAction.CellMouseClick

        WorkWithChannel = e.RowIndex + 1                                                                        ' Change Working Channel

        If tmrAutoRun.Enabled = False Then
            Call VisiblesAtScreen()

            If SettingsAsk Then                                                                                     ' Works Only if Already Asks Pressetings.
                With MyComm.Channel(WorkWithChannel).ActionPoint
                    Dim OnMasterChannel As Boolean = False
                    If .UseNozzle = 1 Then
                        OnMasterChannel = True                                                                                  ' This Channel is Master.
                    End If

                    If .DeviceModel > UBound(MyComm.DeviceModel) Then
                        .DeviceModel = 0
                        txtExplane.Text = "Pomp Model Error. Please Replace..."
                        Beep()
                    End If
                    cmbDeviceModel.SelectedIndex = MyComm.Channel(WorkWithChannel).ActionPoint.DeviceModel                       ' Update Combo Device Model.

                    If MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "Italiana" Then                                       ' Normal Value 0 - 99999
                        txtIdentity.Text = .UseIdentity                                                                         ' Update Text Identity. 
                        txtIdentity.Visible = OnMasterChannel
                        cmbIdentity.Visible = False
                    ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "AK" Then                                       ' Normal Value 0 - 99999
                        txtIdentity.Text = .UseIdentity                                                                         ' Update Text Identity. 
                        txtIdentity.Visible = OnMasterChannel
                        cmbIdentity.Visible = False
                    ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) <> "Not Used" Then
                        If .UseIdentity > 99 Then                                                                               ' Normal Value 00 - 99
                            .UseIdentity = 0
                            txtExplane.Text = "Identity Error. Please Replace..."
                            Beep()
                        End If
                        cmbIdentity.Visible = True
                        cmbIdentity.SelectedIndex = .UseIdentity                                                                ' Update Combo Identity.
                        txtIdentity.Visible = False
                        cmbIdentity.Visible = OnMasterChannel
                    End If

                    If .UseNozzle > UBound(MyComm.AllNozzles) Then
                        .UseNozzle = 0                                                                                          ' "1", "2", "3", "4", "5", "6"
                        txtExplane.Text = "Error Nozzle Nr. Please Replace..."
                        Beep()
                    End If

                    If OnMasterChannel Then                                                                                      ' If Master Channel.
                        cmbTotalNozzles.SelectedIndex = (MyComm.Channel(WorkWithChannel).ActionPoint.NozzlesNozzle \ &H10) - 1
                    End If
                    cmbTotalNozzles.Visible = OnMasterChannel

                    If .CommPort > UBound(MyComm.CommPort) Then
                        .CommPort = 0                                                   ' "0", "1", "2", "3", "4", "5", "6", "7" 
                        txtExplane.Text = "Error Comm. Port. Please Replace..."
                        Beep()
                    End If
                    cmbCommPort.SelectedIndex = .CommPort                               ' Update Combo => Communication Port.
                    cmbCommPort.Visible = OnMasterChannel

                    If .CommStyle > UBound(MyComm.CommStyle) Then
                        .CommStyle = 0                                                  ' "485", "422"
                        txtExplane.Text = "Error Communication Style. Please Replace..."
                        Beep()
                    End If
                    cmbCommStyle.SelectedIndex = .CommStyle                             ' Update Combo => Communication Style.
                    cmbCommStyle.Visible = OnMasterChannel

                    If .CommSpeed > UBound(MyComm.CommSpeed) Then
                        .CommSpeed = 0                                                  ' Auto", "1200", "2400", "4800", "5787", "9600", "19200"
                        txtExplane.Text = "Error Communication Speed. Please Replace..."
                        Beep()
                    End If
                    cmbCommSpeed.SelectedIndex = .CommSpeed                             ' Update Combo => Communication Speed.
                    cmbCommSpeed.Visible = OnMasterChannel

                    If .CommDatas > UBound(MyComm.CommDatas) Then
                        .CommDatas = 0                                                  ' "Auto", "8,n,1", "8,e,1", "8,o,1", "7,n,1", "7,e,1", "7,o,1", "8,n,2", "8,e,2", "8,o,2", "7,n,2", "7,e,2", "7,o,2"
                        txtExplane.Text = "Error Communication Data. Please Replace..."
                        Beep()
                    End If
                    cmbCommDatas.SelectedIndex = .CommDatas                             ' Update Combo Communication Send Datas.
                    cmbCommDatas.Visible = OnMasterChannel

                    If .Product > UBound(MyComm.Products) Then
                        .Product = 0                                                    ' "Χωρίς", "Αμόλ.95", "Αμόλ.95+", "Αμόλ.100", "Super", "Κίνησης", "Κίν.Prem", "Θέρμανσης", "Θέρ.Prem", "Κηροζήνη", "Υγραέριο", "Μίγμα_99"
                        txtExplane.Text = "Error Selected Product. Please Replace..."
                        Beep()
                    End If
                    cmbProducts.SelectedIndex = .Product                                ' Update Combo Products.
                    'cmbProducts.Visible = OnMasterChannel

                End With
            End If
        End If
    End Sub

    ' Preparate Output Transparent String Using Input Transparent String. Exable input String => &M00003, $0D, $0A
    Private Sub tmrExtra_Tick(sender As System.Object, e As System.EventArgs) Handles tmrExtra.Tick

        If StatusEventTime > 0 Then
            StatusEventTime = StatusEventTime - 1
        Else
            txtStatusEvent.BackColor = Color.White
        End If
        If PressetsEventTime > 0 Then
            PressetsEventTime = PressetsEventTime - 1
        Else
            txtPressetsEvent.BackColor = Color.White
        End If
        If DeliveryEventTime > 0 Then
            DeliveryEventTime = DeliveryEventTime - 1
        Else
            txtDeliveryEvent.BackColor = Color.White
        End If
        If TotalsEventTime > 0 Then
            TotalsEventTime = TotalsEventTime - 1
        Else
            txtTotalsEvent.BackColor = Color.White
        End If

        If tmrAutoRun.Enabled = False Then                                          ' Don't Update if Already Runs The Autorun Timer.

            Dim TopicStr As String                                                  ' Helping Variable
            Dim SpareOutputStr As String = ""                                       ' Spare Helping Output String
            Dim OneByte As String                                                   ' Topic
            Dim AnErrorFound As Boolean = False                                     ' Indicate if An Error Found During Translation.

            If Len(txtOutTransparent.Text) <> 0 Then
                cmdSendTrans.Visible = Not tmrAutoRun.Enabled
            Else
                cmdSendTrans.Visible = False
                Exit Sub
            End If

            'If Len(txtOutTransparent.Text) = 0 Then Exit Sub

            TopicStr = txtOutTransparent.Text                                        ' Use Input Transparent String.

            Do Until Len(TopicStr) = 0
                OneByte = ""                                                        ' Set Helping Variable OneByte = Nothing.
                Do
                    If Len(TopicStr) = 0 Then
                        Exit Do                                                     ' If Not More Input String -> EXIT DO
                    ElseIf Mid(TopicStr, 1, 1) = "," Then
                        TopicStr = Mid(TopicStr, 2)                                 ' If Starts with Comma -> EXIT DO
                        Exit Do
                    ElseIf Mid(TopicStr, 1, 1) = " " Then
                        TopicStr = Mid(TopicStr, 2)                                 ' If Starts with Space -> Extract This Space.
                    Else
                        OneByte = OneByte & Mid(TopicStr, 1, 1)                     ' Else Get The First Character from Input String.
                        TopicStr = Mid(TopicStr, 2)                                 ' And Remove This Character on Frond of Input String.
                    End If
                Loop

                If Mid(OneByte, 1, 1) = "$" Then                                    ' Hex Data Value Starts with $       -- One Byte Any Time   Ex. $3 or $A5
                    OneByte = Mid(OneByte, 2)                                       ' Extract "$"
                    If Len(OneByte) = 1 Then
                        SpareOutputStr = SpareOutputStr & "0" & OneByte             ' If Lenght of OneByte = 1.
                    ElseIf Len(OneByte) = 2 Then
                        SpareOutputStr = SpareOutputStr & OneByte                   ' If Lenght of OneByte = 2.
                    Else
                        txtExplane.Text = "Unknow Hex Value. Use $00 to $FF."       ' Else => ERROR
                        AnErrorFound = True
                    End If

                ElseIf Mid(OneByte, 1, 1) = "&" Then                                ' String Data Value Starts with &   -- Any Bytes Any Time   Ex. 123AGT?/
                    OneByte = Mid(OneByte, 2)                                       ' Extract "&"
                    Do
                        If Len(OneByte) = 0 Then
                            Exit Do
                        Else
                            SpareOutputStr = SpareOutputStr & Hex(Asc(Mid(OneByte, 1, 1)))
                        End If
                        OneByte = Mid(OneByte, 2)
                    Loop

                Else                                                                ' Else Decimal Data Value           -- One Byte Any Time    Ex. 0 or 23 or 245
                    Select Case Mid(OneByte, 1, 1)
                        Case 0 To 9
                            If OneByte >= 0 And OneByte <= 255 Then                 ' Normal Value 0-255
                                OneByte = Hex(OneByte)                              ' Convert to Hex
                                If Len(OneByte) = 1 Then
                                    SpareOutputStr = SpareOutputStr & "0" & OneByte ' If Lenght of OneByte = 1.
                                ElseIf Len(OneByte) = 2 Then
                                    SpareOutputStr = SpareOutputStr & OneByte       ' If Lenght of OneByte = 2.
                                End If
                            Else
                                txtExplane.Text = "Big Value. Use 0 to 255."        ' Else => ERROR
                                AnErrorFound = True
                            End If
                        Case Else
                            txtExplane.Text = "Unknow Value. '" & OneByte & "'"
                            AnErrorFound = True
                    End Select

                End If
            Loop

            If AnErrorFound = True Then
                Beep()
            Else
                'txtExplane.Text = ""
            End If

            'txtHexTransparent.Text = ""
            '   ---- Preparate Head. ----
            TransparentLength = 10 + (Len(SpareOutputStr) / 2)                                      ' All Packet Length = 8(Head) + Data + 2(Checksum)
            txtHexTransparent.Text = "&HAA,&H55,&H00,&H" & Hex(TransparentLength) & ",&H45,&H53"    ' Preparate Output String

            TopicStr = Hex(WorkWithChannel - 1)                                                     ' To Channel.
            If Len(TopicStr) = 1 Then TopicStr = "0" & TopicStr
            TopicStr = ",&H" & TopicStr                                                             ' Add to Channel.

            txtHexTransparent.Text = txtHexTransparent.Text & TopicStr
            txtHexTransparent.Text = txtHexTransparent.Text & ",&H00"                               ' 00 = Transparent Mode.

            '   ---- Add and Trasparent Data. ----
            For MetA = 1 To Len(SpareOutputStr) \ 2
                txtHexTransparent.Text = txtHexTransparent.Text & ",&H" & Mid(SpareOutputStr, 1, 2)
                SpareOutputStr = Mid(SpareOutputStr, 3)
            Next

        Else

        End If

    End Sub

    ' Preparate and Sends a Transparent String to Controler.
    Private Sub cmdTransparent_Click(sender As System.Object, e As System.EventArgs) Handles cmdSendTrans.Click

        Dim AllString As String
        Dim ByteStr As String

        Dim StoreTo As Long = 0                                                     ' Start to Store Bytes to CommData Array from 0.
        AllString = txtHexTransparent.Text                                          ' Get The String From Text Box.

        Do Until Len(AllString) = 0
            ByteStr = GetOneDataStr(AllString)                                      ' Get One Byte
            MyComm.CommData(StoreTo) = Chr(CLng(ByteStr))                           ' Update CommData Array.
            StoreTo = StoreTo + 1
        Loop

        ' The Following 3 Lines are Nessasary Because Needed to Make Transmit Packet Function.
        MyComm.Communication.OutComLength = TransparentLength                       ' Total Bytes For Send = 8 + Data + 2       Examble : &M00003, $0D, $0A => "&"+String or "$"+Hex Byte
        MyComm.Communication.OutComChannel = WorkWithChannel - 1                    ' For Channel.(00 - Max.Channels -1)
        MyComm.Communication.OutComInfos = 0                                        ' 00=Transparent, (01=Unit Price, 02=Preset Volume, 02=Preset Amount.)

        MyComm.Communication.InComWaitBytes = &HFF                                  ' The Answer must Contain ??? (Unknow) Bytes. 

        Call MyComm.SendPacketToControler()

    End Sub

    Private Function GetOneDataStr(ByRef FromStr As String) As String
        GetOneDataStr = ""

        Do Until Mid(FromStr, 1, 1) = ","
            If Len(FromStr) > 0 Then
                GetOneDataStr = GetOneDataStr & Mid(FromStr, 1, 1)
                FromStr = Mid(FromStr, 2, Len(FromStr) - 1)
            Else
                Exit Function
            End If
        Loop

        If Mid(FromStr, 1, 1) = "," Then
            FromStr = Mid(FromStr, 2, Len(FromStr) - 1)                             ' Extract if Found "," at Start of Rest String
        End If

    End Function

    '   ---- On Channel Status Something => Change. ----
    Private Sub MyComm_OnStatusChange(AnswerOnExplane As Object) Handles MyComm.OnStatusChange                  ' EVENT

        StatusEventTime = 1
        txtStatusEvent.BackColor = Color.Red

        For MetB = 1 To UBound(MyComm.Channel)                                                                  ' For All Channels
            With MyComm.Channel(MetB).ActionPoint.APointStatus
                If MyComm.Channel(MetB).ActionPoint.DeviceModel > 0 Then                                        ' Update Grid if There is Pump Model.
                    gridAction.Item("Master", MetB - 1).Value = IIf(.IsMaster, "MS", "")
                    gridAction.Item("Master", MetB - 1).Style.BackColor = IIf(.IsMaster, Color.GreenYellow, Color.Lavender)
                    If .IsMaster = True Then
                        gridAction.Item("Alive", MetB - 1).Value = IIf(.NozzleAlive, "YES", "NO")
                        gridAction.Item("Alive", MetB - 1).Style.BackColor = IIf(.NozzleAlive, Color.GreenYellow, Color.Khaki)
                        gridAction.Item("Block", MetB - 1).Value = IIf(.NozzleBlocked, "YES", "NO")
                        gridAction.Item("Block", MetB - 1).Style.BackColor = IIf(.NozzleBlocked, Color.Khaki, Color.GreenYellow)
                        gridAction.Item("Auth", MetB - 1).Value = IIf(.Authorized, "YES", "NO")
                        gridAction.Item("Auth", MetB - 1).Style.BackColor = IIf(.Authorized, Color.GreenYellow, Color.Khaki)
                        gridAction.Item("GetTra", MetB - 1).Value = IIf(.GetTransaction, "YES", "NO")
                        gridAction.Item("GetTra", MetB - 1).Style.BackColor = IIf(.GetTransaction, Color.GreenYellow, Color.Khaki)
                        gridAction.Item("NeedA", MetB - 1).Value = IIf(.RequestAuthorize, "YES", "NO")
                        gridAction.Item("NeedA", MetB - 1).Style.BackColor = IIf(.RequestAuthorize, Color.GreenYellow, Color.Khaki)
                        gridAction.Item("OnFill", MetB - 1).Value = IIf(.Filling, "YES", "NO")
                        gridAction.Item("OnFill", MetB - 1).Style.BackColor = IIf(.Filling, Color.GreenYellow, Color.Khaki)
                        gridAction.Item("NzOut", MetB - 1).Value = IIf(.NozzleOut, "YES", "NO")
                        gridAction.Item("NzOut", MetB - 1).Style.BackColor = IIf(.NozzleOut, Color.GreenYellow, Color.Khaki)

                        gridAction.Item("PStat", MetB - 1).Value = Hex(MyComm.Channel(MetB).ActionPoint.PumpStatusNow)                  ' Pump Status Now
                        gridAction.Item("PErr", MetB - 1).Value = Hex(MyComm.Channel(MetB).ActionPoint.PumpErrorCode)                   ' Last Error on Pump.
                        gridAction.Item("APStat", MetB - 1).Value = Hex(MyComm.Channel(MetB).ActionPoint.APointStatus.Value)            ' Action Point Status.

                    Else
                        gridAction.Item("Master", MetB - 1).Value = ""
                        gridAction.Item("Master", MetB - 1).Style.BackColor = Color.Goldenrod
                        gridAction.Item("Alive", MetB - 1).Value = ""
                        gridAction.Item("Alive", MetB - 1).Style.BackColor = Color.Goldenrod
                        gridAction.Item("Block", MetB - 1).Value = ""
                        gridAction.Item("Block", MetB - 1).Style.BackColor = Color.Goldenrod
                        gridAction.Item("Auth", MetB - 1).Value = ""
                        gridAction.Item("Auth", MetB - 1).Style.BackColor = Color.Goldenrod
                        gridAction.Item("GetTra", MetB - 1).Value = ""
                        gridAction.Item("GetTra", MetB - 1).Style.BackColor = Color.Goldenrod
                        gridAction.Item("NeedA", MetB - 1).Value = ""
                        gridAction.Item("NeedA", MetB - 1).Style.BackColor = Color.Goldenrod
                        gridAction.Item("OnFill", MetB - 1).Value = ""
                        gridAction.Item("OnFill", MetB - 1).Style.BackColor = Color.Goldenrod
                        gridAction.Item("NzOut", MetB - 1).Value = ""
                        gridAction.Item("NzOut", MetB - 1).Style.BackColor = Color.Goldenrod

                        gridAction.Item("PStat", MetB - 1).Value = ""
                        gridAction.Item("PStat", MetB - 1).Style.BackColor = Color.Goldenrod
                        gridAction.Item("PErr", MetB - 1).Value = ""
                        gridAction.Item("PErr", MetB - 1).Style.BackColor = Color.Goldenrod
                        gridAction.Item("APStat", MetB - 1).Value = ""
                        gridAction.Item("APStat", MetB - 1).Style.BackColor = Color.Goldenrod

                    End If
                End If
            End With

        Next

    End Sub

    '   ---- On Channel Pressetings Something => Change. ----
    Private Sub MyComm_OnPressetingsChange(AnswerOnExplane As Object) Handles MyComm.OnPressetingsChange        ' EVENT

        PressetsEventTime = 1
        txtPressetsEvent.BackColor = Color.Red
        Dim TheColor As Color

        For MetB = 1 To UBound(MyComm.Channel)                                                      ' For All Channels

            Select Case MyComm.DeviceModel(MyComm.Channel(MetB).ActionPoint.DeviceModel)
                Case "Not Used"
                    TheColor = Color.Black
                Case "Transparent"
                    TheColor = Color.Black
                Case "Pignone"
                    TheColor = Color.Red
                Case "Wayne"
                    TheColor = Color.Red
                Case "Prime"
                    TheColor = Color.Green
                Case "Gilbarco"
                    TheColor = Color.Blue
                Case "GVR"
                    TheColor = Color.Fuchsia
                Case "Tokheim"
                    TheColor = Color.Red
                Case "Italiana"
                    TheColor = Color.DarkViolet
                Case "AK"
                    TheColor = Color.DarkOrchid
                Case "RousisRFID"
                    TheColor = Color.DarkOrchid
                Case Else
                    Stop
            End Select

            For ColMet = 1 To gridAction.ColumnCount - 1
                gridAction.Item(ColMet, MetB - 1).Style.ForeColor = TheColor                            ' Set This Fore Color on a Row.
            Next

            With MyComm.Channel(MetB).ActionPoint
                If .DeviceModel > 0 Then
                    If .APointStatus.IsMaster = True Then
                        gridAction.Item("NzNz", MetB - 1).Value = Hex(.NozzlesNozzle)                   ' TotalNozzles/LastNozzleUp
                        gridAction.Item("Points", MetB - 1).Value = .DecimalPoints.UnitPrice & "/" & .DecimalPoints.Amount & "/" & .DecimalPoints.Volume
                    Else
                        gridAction.Item("NzNz", MetB - 1).Value = ""
                        gridAction.Item("NzNz", MetB - 1).Style.BackColor = Color.Goldenrod
                        gridAction.Item("Points", MetB - 1).Value = ""
                        gridAction.Item("Points", MetB - 1).Style.BackColor = Color.Goldenrod
                    End If
                    gridAction.Item("Pomp", MetB - 1).Value = MyComm.DeviceModel(.DeviceModel)      ' Device Model.                 (From Array)
                    gridAction.Item("Iden", MetB - 1).Value = .UseIdentity                          ' Identity
                    gridAction.Item("Nozz", MetB - 1).Value = .UseNozzle                            ' Nozzle Number
                    gridAction.Item("Port", MetB - 1).Value = .CommPort                             ' Comm. Port Nr
                    gridAction.Item("Styl", MetB - 1).Value = MyComm.CommStyle(.CommStyle)          ' Comm. Style.                  (From Array)
                    gridAction.Item("Sped", MetB - 1).Value = MyComm.CommSpeed(.CommSpeed)          ' Comm.Speed.                   (From Array)
                    gridAction.Item("Data", MetB - 1).Value = MyComm.CommDatas(.CommDatas)          ' Comm. Data Characterestics.   (From Array)
                    gridAction.Item("Prod", MetB - 1).Value = MyComm.Products(.Product)             ' Product.                      (From Array)

                Else
                    gridAction.Item("NzNz", MetB - 1).Value = ""
                    gridAction.Item("Points", MetB - 1).Value = ""

                    gridAction.Item("Pomp", MetB - 1).Value = "?"
                    gridAction.Item("Iden", MetB - 1).Value = ""
                    gridAction.Item("Nozz", MetB - 1).Value = ""
                    gridAction.Item("Port", MetB - 1).Value = ""
                    gridAction.Item("Styl", MetB - 1).Value = ""
                    gridAction.Item("Sped", MetB - 1).Value = ""
                    gridAction.Item("Data", MetB - 1).Value = ""
                    gridAction.Item("Prod", MetB - 1).Value = ""

                End If
            End With
        Next

        SettingsAsk = True

    End Sub

    ' ---- Displays Unit Price, Displays Amount, Displays Volume, Current Unit Price, Current Amount, Current Volume => Change ----
    Private Sub MyComm_OnDeliveryChange(AnswerOnExplane As Object) Handles MyComm.OnDeliveryChange                                      ' EVENT

        DeliveryEventTime = 1
        txtDeliveryEvent.BackColor = Color.Red

        For MetB = 1 To UBound(MyComm.Channel)                                                      ' For All Channels
            gridAction.Item("DPrice", MetB - 1).Style.BackColor = Color.White
            gridAction.Item("DVolu", MetB - 1).Style.BackColor = Color.White
            gridAction.Item("DAmou", MetB - 1).Style.BackColor = Color.White
            gridAction.Item("Price", MetB - 1).Style.BackColor = Color.White
            gridAction.Item("CAmou", MetB - 1).Style.BackColor = Color.White
            gridAction.Item("CVolu", MetB - 1).Style.BackColor = Color.White

            Select Case MyComm.DeviceModel(MyComm.Channel(MetB).ActionPoint.DeviceModel)
                Case "Not Used", "Transparent"
                    gridAction.Item("DPrice", MetB - 1).Value = ""
                    gridAction.Item("DPrice", MetB - 1).Style.BackColor = Color.Lavender
                    gridAction.Item("DVolu", MetB - 1).Value = ""
                    gridAction.Item("DVolu", MetB - 1).Style.BackColor = Color.Lavender
                    gridAction.Item("DAmou", MetB - 1).Value = ""
                    gridAction.Item("DAmou", MetB - 1).Style.BackColor = Color.Lavender
                    gridAction.Item("Price", MetB - 1).Value = ""
                    gridAction.Item("Price", MetB - 1).Style.BackColor = Color.Lavender
                    gridAction.Item("CAmou", MetB - 1).Value = ""
                    gridAction.Item("CAmou", MetB - 1).Style.BackColor = Color.Lavender
                    gridAction.Item("CVolu", MetB - 1).Value = ""
                    gridAction.Item("CVolu", MetB - 1).Style.BackColor = Color.Lavender

                Case "Italiana"

                    '  Byte Pump Status returns Gauge Status
                    Dim Prefix As String = Chr(MyComm.Channel(MetB).ActionPoint.PumpErrorCode)                                                      ' Prefix on Temperature.( + or -)
                    gridAction.Item("DPrice", MetB - 1).Value = Prefix & (MyComm.Channel(MetB).Delivery.DisplayPrice / 10).ToString("0.0") & "°C"   ' Temperature
                    gridAction.Item("DAmou", MetB - 1).Value = (MyComm.Channel(MetB).Delivery.DisplayAmount / 10).ToString("0.0") & "mm"            ' Products
                    gridAction.Item("DVolu", MetB - 1).Value = (MyComm.Channel(MetB).Delivery.DisplayVolume / 100).ToString("0.0") & "mm"           ' Water

                Case Else
                    If MyComm.Channel(MetB).ActionPoint.APointStatus.IsMaster = True Then
                        gridAction.Item("DPrice", MetB - 1).Value = (MyComm.Channel(MetB).Delivery.DisplayPrice / 1000).ToString("0.000")
                        gridAction.Item("DVolu", MetB - 1).Value = (MyComm.Channel(MetB).Delivery.DisplayVolume / 100).ToString("0.00")
                        gridAction.Item("DAmou", MetB - 1).Value = (MyComm.Channel(MetB).Delivery.DisplayAmount / 100).ToString("0.00")
                    Else
                        gridAction.Item("DPrice", MetB - 1).Value = ""
                        gridAction.Item("DPrice", MetB - 1).Style.BackColor = Color.Goldenrod
                        gridAction.Item("DVolu", MetB - 1).Value = ""
                        gridAction.Item("DVolu", MetB - 1).Style.BackColor = Color.Goldenrod
                        gridAction.Item("DAmou", MetB - 1).Value = ""
                        gridAction.Item("DAmou", MetB - 1).Style.BackColor = Color.Goldenrod
                    End If
                    gridAction.Item("Price", MetB - 1).Value = (MyComm.Channel(MetB).Delivery.CurrentPrice / 1000).ToString("0.000")
                    gridAction.Item("CAmou", MetB - 1).Value = (MyComm.Channel(MetB).Delivery.CurrentAmount / 100).ToString("0.00")
                    gridAction.Item("CVolu", MetB - 1).Value = (MyComm.Channel(MetB).Delivery.CurrentVolume / 100).ToString("0.00")
            End Select
        Next

    End Sub

    '   ---- On Total Amount or Total Volume => Change
    Private Sub MyComm_OnReadTotalsChange(AnswerOnExplane As Object) Handles MyComm.OnReadTotalsChange              ' EVENT

        TotalsEventTime = 1
        txtTotalsEvent.BackColor = Color.Red

        For MetB = 1 To UBound(MyComm.Channel)                                                      ' For All Channels
            gridAction.Item("TAmou", MetB - 1).Style.BackColor = Color.White
            gridAction.Item("TVolu", MetB - 1).Style.BackColor = Color.White

            Select Case MyComm.DeviceModel(MyComm.Channel(MetB).ActionPoint.DeviceModel)
                Case "Not Used", "Transparent", "Italiana"
                    gridAction.Item("TAmou", MetB - 1).Value = ""
                    gridAction.Item("TAmou", MetB - 1).Style.BackColor = Color.Lavender
                    gridAction.Item("TVolu", MetB - 1).Value = ""
                    gridAction.Item("TVolu", MetB - 1).Style.BackColor = Color.Lavender

                Case Else
                    gridAction.Item("TAmou", MetB - 1).Value = (MyComm.Channel(MetB).Totals.TotalAmount / 100).ToString("0.00")
                    gridAction.Item("TVolu", MetB - 1).Value = (MyComm.Channel(MetB).Totals.TotalVolume / 100).ToString("0.00")

            End Select
        Next

    End Sub

    Private Sub MyComm_Nozzle_Change(OnChannel As Integer) Handles MyComm.Nozzle_Change                             ' EVENT
        My.Computer.Audio.Play(My.Resources.ding, AudioPlayMode.WaitToComplete)
    End Sub

    Private Sub MyComm_USB_ErrorFound(Err_Description As String) Handles MyComm.USB_ErrorFound                      ' EVENT
        lboxInformations.Items.Add(Err_Description)
    End Sub

    Private Sub MyComm_TrasparentAnswer(Err_Description As String) Handles MyComm.TrasparentAnswer                  ' EVENT
        txtOutTransparent.Text = ""
        txtAsciiTransparent.Text = MyComm.Transparent.IncomAscii
        txtHexTransparent.Text = MyComm.Transparent.IncomHex
    End Sub

    Private Sub cmbPompModel_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cmbDeviceModel.SelectedIndexChanged

        If tmrAutoRun.Enabled = False Then

            If MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "Not Used" Then
                cmbIdentity.Visible = False
                txtIdentity.Visible = False
                cmbTotalNozzles.Visible = False
                cmbCommPort.Visible = False
                cmbCommStyle.Visible = False
                cmbCommSpeed.Visible = False
                cmbCommDatas.Visible = False
                cmbProducts.Visible = False
            Else
                cmbIdentity.Visible = True
                txtIdentity.Visible = True
                cmbTotalNozzles.Visible = True
                cmbCommPort.Visible = True
                cmbCommStyle.Visible = True
                cmbCommSpeed.Visible = True
                cmbCommDatas.Visible = True
                cmbProducts.Visible = True
            End If

            If MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "Not Used" Then
                '

            ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "Transparent" Then
                cmbIdentity.Items.Clear()
                For MetID = 0 To 0
                    cmbIdentity.Items.Add(CStr(MetID))                                          ' Transparent Has Identity 0
                Next
                cmbTotalNozzles.Items.Clear()
                cmbTotalNozzles.Items.Add("1")

            ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "Pignone" Then
                cmbIdentity.Items.Clear()
                For MetID = 0 To 31
                    cmbIdentity.Items.Add(CStr(MetID))                                          ' Pignone Has Identity 1 to 31
                Next
                cmbTotalNozzles.Items.Clear()
                cmbTotalNozzles.Items.Add("1")
                cmbTotalNozzles.Items.Add("2")
                cmbTotalNozzles.Items.Add("3")
                cmbTotalNozzles.Items.Add("4")

            ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "Wayne" Then
                cmbIdentity.Items.Clear()
                For MetID = 0 To 99
                    cmbIdentity.Items.Add(CStr(MetID))                                          ' Wayne Has Identity 1 to 99
                Next
                cmbTotalNozzles.Items.Clear()
                cmbTotalNozzles.Items.Add("1")
                cmbTotalNozzles.Items.Add("2")
                cmbTotalNozzles.Items.Add("3")
                cmbTotalNozzles.Items.Add("4")

            ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "Prime" Then
                cmbIdentity.Items.Clear()
                For MetID = 0 To 99
                    cmbIdentity.Items.Add(CStr(MetID))                                          ' Prime Has Identity 1 to 99
                Next
                cmbTotalNozzles.Items.Clear()
                cmbTotalNozzles.Items.Add("1")

            ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "Gilbarco" Then
                cmbIdentity.Items.Clear()
                For MetID = 0 To 16
                    cmbIdentity.Items.Add(CStr(MetID))                                          ' Gilbarco Has Identity 1 to 16
                Next
                cmbTotalNozzles.Items.Clear()
                cmbTotalNozzles.Items.Add("1")
                cmbTotalNozzles.Items.Add("2")
                cmbTotalNozzles.Items.Add("3")

            ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "GVR" Then
                cmbIdentity.Items.Clear()
                For MetID = 0 To 99
                    cmbIdentity.Items.Add(CStr(MetID))                                          ' Pignone Has Identity 1 to 99
                Next
                cmbTotalNozzles.Items.Clear()
                cmbTotalNozzles.Items.Add("1")
                cmbTotalNozzles.Items.Add("2")
                cmbTotalNozzles.Items.Add("3")
                cmbTotalNozzles.Items.Add("4")

            ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "Tokheim" Then
                cmbIdentity.Items.Clear()
                For MetID = 0 To 99
                    cmbIdentity.Items.Add(CStr(MetID))                                          ' Tokheim Has Identity 1 to 99
                Next
                cmbTotalNozzles.Items.Clear()
                cmbTotalNozzles.Items.Add("1")
                cmbTotalNozzles.Items.Add("2")
                cmbTotalNozzles.Items.Add("3")
                cmbTotalNozzles.Items.Add("4")

            ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "Italiana" Then
                cmbIdentity.Items.Clear()
                For MetID = 0 To 0
                    cmbIdentity.Items.Add(CStr(MetID))                                          ' Italiana Has Identity 3 Bytes.(5 Decimal Digits.)
                Next
                cmbTotalNozzles.Items.Clear()
                cmbTotalNozzles.Items.Add("1")

                cmbIdentity.Visible = False

            ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "AK" Then
                cmbIdentity.Items.Clear()
                For MetID = 0 To 0
                    cmbIdentity.Items.Add(CStr(MetID))                                          ' Transparent Has Identity 0
                Next
                cmbTotalNozzles.Items.Clear()
                cmbTotalNozzles.Items.Add("1")

                cmbIdentity.Visible = False

            ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "RousisRFID" Then
                cmbIdentity.Items.Clear()
                For MetID = 0 To 99
                    cmbIdentity.Items.Add(CStr(MetID))                                          ' RousisRFID Has Identity 3 Bytes.(5 Decimal Digits.)
                Next
                cmbTotalNozzles.Items.Clear()
                cmbTotalNozzles.Items.Add("1")


            Else
                Stop

            End If
        End If
        Call VisiblesAtScreen()
    End Sub

    '   ---- About Controler Description, SerialNr, Reserved1 and Reserved2 Ask and Store. --------
    Private Sub btnGetController_Click(sender As System.Object, e As System.EventArgs) Handles btnGetController.Click
        MyComm.ReadControler_RS(0)     ' Description
        MyComm.ReadControler_RS(1)     ' SerialNr
        MyComm.ReadControler_RS(2)     ' Reserved1
        MyComm.ReadControler_RS(3)     ' Reserved2
    End Sub

    Private Sub MyComm_DescriptionChange(Err_Description As String) Handles MyComm.DescriptionChange                    ' EVENT
        txtDescription.Text = MyComm.Controller.Description
    End Sub

    Private Sub MyComm_SerialNrChange(Err_Description As String) Handles MyComm.SerialNrChange                          ' EVENT
        txtSerialNumber.Text = MyComm.Controller.SerialNr
    End Sub

    Private Sub MyComm_Reserved1Change(Err_Description As String) Handles MyComm.Reserved1Change                        ' EVENT
        txtReserved1.Text = MyComm.Controller.Reserved1
    End Sub

    Private Sub MyComm_Reserved2Change(Err_Description As String) Handles MyComm.Reserved2Change                        ' EVENT
        txtReserved2.Text = MyComm.Controller.Reserved2
    End Sub

    Private Sub MyComm_OnChangeUnitPrice(Answer As String) Handles MyComm.OnChangeUnitPrice                             ' EVENT
        lboxInformations.Items.Add(Answer)
    End Sub

    Private Sub MyComm_OnPressetVolume(Answer As String) Handles MyComm.OnPressetVolume                                 ' EVENT
        lboxInformations.Items.Add(Answer)
    End Sub

    Private Sub MyComm_OnPressetAmount(Answer As String) Handles MyComm.OnPressetAmount                                 ' EVENT
        lboxInformations.Items.Add(Answer)
    End Sub

    Private Sub MyComm_ControllerRestart(Answer As String) Handles MyComm.ControllerRestart                             ' EVENT
        lboxInformations.Items.Add(Answer)
    End Sub

    Private Sub cmdDescription_Click(sender As System.Object, e As System.EventArgs) Handles cmdDescription.Click
        MyComm.WriteController_WS(0, txtDescription.Text)
    End Sub

    Private Sub cmdSerialNumber_Click(sender As System.Object, e As System.EventArgs) Handles cmdSerialNumber.Click
        MyComm.WriteController_WS(1, txtSerialNumber.Text)
    End Sub

    Private Sub cmdReserved1_Click(sender As System.Object, e As System.EventArgs) Handles cmdReserved1.Click
        MyComm.WriteController_WS(2, txtReserved1.Text)
    End Sub

    Private Sub cmdReserved2_Click(sender As System.Object, e As System.EventArgs) Handles cmdReserved2.Click
        MyComm.WriteController_WS(3, txtReserved2.Text)
    End Sub

    '   ---- On Double Click Clear List Box. -----------------------------------------------------
    Private Sub lboxUSB_Errors_DoubleClick(sender As Object, e As System.EventArgs) Handles lboxInformations.DoubleClick
        lboxInformations.Items.Clear()
    End Sub

    Private Sub VisiblesAtScreen(Optional ForceUnvisible As Boolean = False)

        If tmrAutoRun.Enabled = True Then
            cmdAutoRun.Text = "STOP"
        Else
            cmdAutoRun.Text = "RUN"
        End If

        Dim VisibleUnvisible As Boolean
        If ForceUnvisible = True Then
            VisibleUnvisible = False
        Else
            VisibleUnvisible = Not tmrAutoRun.Enabled
        End If

        gridAction.Visible = Not ForceUnvisible : lblRunInfo.Visible = Not ForceUnvisible
        lblChannel.Visible = Not ForceUnvisible
        lblWorkWithChannel.Visible = Not ForceUnvisible : txtStatusEvent.Visible = Not ForceUnvisible
        lblWorkWithChannel.Text = WorkWithChannel : txtPressetsEvent.Visible = Not ForceUnvisible
        cmdAutoRun.Visible = Not ForceUnvisible : txtDeliveryEvent.Visible = Not ForceUnvisible
        btnRestart.Visible = Not ForceUnvisible : txtTotalsEvent.Visible = Not ForceUnvisible
        cmdClearList.Visible = Not ForceUnvisible : btnBlock.Visible = Not ForceUnvisible
        txtElapseSeconds.Visible = Not ForceUnvisible : btnResume.Visible = Not ForceUnvisible
        txtActionTimes.Visible = Not ForceUnvisible : txtBlock.Visible = Not ForceUnvisible
        lboxInformations.Visible = Not ForceUnvisible
        txtExplane.Visible = Not ForceUnvisible

        cmdSendTrans.Visible = VisibleUnvisible
        txtHexTransparent.Visible = VisibleUnvisible
        txtOutTransparent.Visible = VisibleUnvisible
        txtAsciiTransparent.Visible = VisibleUnvisible

        cmdChangePrice.Visible = Not ForceUnvisible : txtChangePrice.Visible = Not ForceUnvisible
        cmdPresetVolume.Visible = Not ForceUnvisible : txtPresetVolume.Visible = Not ForceUnvisible
        cmdPresetAmount.Visible = Not ForceUnvisible : txtPresetAmount.Visible = Not ForceUnvisible

        btnAskSettings.Visible = VisibleUnvisible : btnAskSettingsUsed.Visible = VisibleUnvisible
        btnAskDeliveries.Visible = VisibleUnvisible : btnAskDeliveriesUsed.Visible = VisibleUnvisible
        btnGetTotals.Visible = VisibleUnvisible : btnGetTotalsUsed.Visible = VisibleUnvisible

        cmbDeviceModel.Visible = VisibleUnvisible

        If cmbDeviceModel.SelectedIndex > -1 Then
            If tmrAutoRun.Enabled = True Then
                cmbIdentity.Visible = False
                txtIdentity.Visible = False
            Else
                If MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "Not Used" Then
                    cmbIdentity.Visible = VisibleUnvisible
                    txtIdentity.Visible = VisibleUnvisible
                ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "Italiana" Then
                    cmbIdentity.Visible = False
                    txtIdentity.Visible = True
                ElseIf MyComm.DeviceModel(cmbDeviceModel.SelectedIndex) = "AK" Then
                    cmbIdentity.Visible = False
                    txtIdentity.Visible = True
                Else
                    cmbIdentity.Visible = True
                    txtIdentity.Visible = False
                End If
            End If
        Else
            cmbIdentity.Visible = False
            txtIdentity.Visible = False
        End If

        cmbTotalNozzles.Visible = VisibleUnvisible
        cmbCommPort.Visible = VisibleUnvisible
        cmbCommStyle.Visible = VisibleUnvisible
        cmbCommSpeed.Visible = VisibleUnvisible
        cmbCommDatas.Visible = VisibleUnvisible
        cmbProducts.Visible = VisibleUnvisible
        cmdStoreSets.Visible = VisibleUnvisible

        btnGetController.Visible = VisibleUnvisible
        txtDescription.Visible = VisibleUnvisible
        txtSerialNumber.Visible = VisibleUnvisible
        txtReserved1.Visible = VisibleUnvisible
        txtReserved2.Visible = VisibleUnvisible
        cmdDescription.Visible = VisibleUnvisible
        cmdSerialNumber.Visible = VisibleUnvisible
        cmdReserved1.Visible = VisibleUnvisible
        cmdReserved2.Visible = VisibleUnvisible

    End Sub



End Class