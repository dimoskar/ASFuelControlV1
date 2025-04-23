Public Class frmMaster

    Private Sub Form1_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
        WorkingWithUSB_Nr = -1                          ' Set Working with USB Number = Error Nr.
        ToolStrip_Controler.Visible = False             ' Close Menu Controler Until Select a Normal USB Number.
        'Call DisplayActionForm()
        frmAction.Show()
    End Sub

    Private Sub ToolStrip_Select_USB_Click(sender As System.Object, e As System.EventArgs) Handles ToolStrip_Select_USB.Click
        Call DisplayUSBForm()
    End Sub

    Private Sub ToolStripButton1_Click(sender As System.Object, e As System.EventArgs) Handles ToolStrip_Controler.Click
        frmAction.tmrExtra.Enabled = False
        frmAction.tmrAutoRun.Enabled = False
        TimeStart = DateTime.Now                        ' Get Current Time Date
        frmMyFTDI.Hide()                                ' Hide Form FTDI
        frmAction.MdiParent = Me
        frmAction.Show()                                ' Show Form Action
        Call frmAction.UpdateAction()                   ' Update Form Action.
    End Sub

    Private Sub DisplayUSBForm()
        frmAction.tmrExtra.Enabled = False
        frmAction.tmrAutoRun.Enabled = False
        frmAction.Hide()                                ' Hide Form Action
        frmMyFTDI.MdiParent = Me
        frmMyFTDI.Show()                                ' Show Form FTDI
        Call frmMyFTDI.UpdateUSB_Scan()                 ' Update Form FTDI.
    End Sub

    Private Sub DisplayActionForm()
        frmAction.tmrExtra.Enabled = False
        frmAction.tmrAutoRun.Enabled = False
        TimeStart = DateTime.Now                        ' Get Current Time Date
        frmMyFTDI.Hide()                                ' Hide Form FTDI
        frmAction.MdiParent = Me
        frmAction.Show()                                ' Show Form Action
        Call frmAction.UpdateAction()                   ' Update Form Action.
    End Sub

End Class
