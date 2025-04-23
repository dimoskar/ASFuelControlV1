
Public Class frmMyFTDI

    Private MyComm As New ElbisDLL_New.clsComm                                  ' Declare the Communication Class on ElbisMyDLL Library.

    Public Sub UpdateUSB_Scan()

        'Dim MetA As Integer = 0
        WorkingWithUSB_Nr = 0
        'lblWorkWithNr.Text = WorkingWithUSB_Nr.ToString
        frmMaster.ToolStrip_Controler.Visible = False
        'txtDescription.Text = ""
        'txtSerialNumber.Text = ""
        'lblPleaseSelect.Text = "PLEASE WAIT"

        gridUSB_Scan.Rows.Clear()
        Call MyFTDI.ScanAllDevices(TotalDevices)

        If TotalDevices > 0 Then                                                                                        ' If Found Devices
            For MetA = 0 To TotalDevices - 1                                                                            ' For All Pressent Devices
                gridUSB_Scan.Rows.Add()
                gridUSB_Scan.Item(0, MetA).Value = MetA + 1
                gridUSB_Scan.Item("Description", MetA).Value = MyFTDI.FTDI_Device(MetA + 1).Description
                gridUSB_Scan.Item("Serial_Number", MetA).Value = MyFTDI.FTDI_Device(MetA + 1).SerialNumber
                'gridUSB_Scan.Item("Description_Error", MetA).Value = MyFTDI.FTDI_Device(MetA + 1).DescriptionError
                'gridUSB_Scan.Item("Serial_Number_Error", MetA).Value = MyFTDI.FTDI_Device(MetA + 1).SerialNumberError
            Next
        Else
            'lblPleaseSelect.Text = "No Device Found."
            Exit Sub
        End If
        'lblPleaseSelect.Text = "Please Select Device"

    End Sub

    'Private Sub gridUSB_Scan_CellMouseClick(sender As Object, e As System.Windows.Forms.DataGridViewCellMouseEventArgs) Handles gridUSB_Scan.CellMouseClick

    '    Try
    '        WorkingWithUSB_Nr = gridUSB_Scan.Item(0, e.RowIndex).Value - 1

    '        'lblWorkWithNr.Text = WorkingWithUSB_Nr + 1.ToString
    '        'txtDescription.Text = gridUSB_Scan.Item(1, e.RowIndex).Value
    '        'txtSerialNumber.Text = gridUSB_Scan.Item(2, e.RowIndex).Value
    '        If WorkingWithUSB_Nr > -1 Then
    '            frmMaster.ToolStrip_Controler.Visible = True
    '        Else
    '            frmMaster.ToolStrip_Controler.Visible = False
    '        End If
    '    Catch ex As Exception
    '        Beep()
    '    End Try

    'End Sub

    Private Sub gridUSB_Scan_CellMouseDoubleClick(sender As Object, e As System.Windows.Forms.DataGridViewCellMouseEventArgs) Handles gridUSB_Scan.CellMouseDoubleClick

        Try
            WorkingWithUSB_Nr = gridUSB_Scan.Item(0, e.RowIndex).Value - 1

            'lblWorkWithNr.Text = WorkingWithUSB_Nr + 1.ToString
            'txtDescription.Text = gridUSB_Scan.Item(1, e.RowIndex).Value
            'txtSerialNumber.Text = gridUSB_Scan.Item(2, e.RowIndex).Value
            If WorkingWithUSB_Nr > -1 Then
                frmMaster.ToolStrip_Controler.Visible = True
            Else
                frmMaster.ToolStrip_Controler.Visible = False
            End If
        Catch ex As Exception
            Beep()
            Exit Sub
        End Try

        frmAction.tmrExtra.Enabled = False
        frmAction.tmrAutoRun.Enabled = False
        TimeStart = DateTime.Now                        ' Get Current Time Date
        Me.Hide()                                       ' Hide Form FTDI
        frmAction.MdiParent = frmMaster
        frmAction.Show()                                ' Show Form Action
        Call frmAction.UpdateAction()                   ' Update Form Action.
    End Sub

    'Private Sub gridUSB_Scan_RowHeaderMouseClick(sender As Object, e As System.Windows.Forms.DataGridViewCellMouseEventArgs) Handles gridUSB_Scan.RowHeaderMouseClick

    '    Try
    '        WorkingWithUSB_Nr = gridUSB_Scan.Item(0, e.RowIndex).Value - 1

    '        lblWorkWithNr.Text = WorkingWithUSB_Nr + 1.ToString
    '        txtDescription.Text = gridUSB_Scan.Item(1, e.RowIndex).Value
    '        txtSerialNumber.Text = gridUSB_Scan.Item(2, e.RowIndex).Value
    '        If WorkingWithUSB_Nr > -1 Then
    '            frmMaster.ToolStrip_Controler.Visible = True
    '        Else
    '            frmMaster.ToolStrip_Controler.Visible = False
    '        End If
    '    Catch ex As Exception
    '        Beep()
    '    End Try

    'End Sub

End Class