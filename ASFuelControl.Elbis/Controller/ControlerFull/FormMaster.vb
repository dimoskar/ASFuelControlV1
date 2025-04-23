

Public Class frmMaster

    Private Sub Form1_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
        WorkingWithUSB_Nr = -1                          ' Set Working with USB Number = Error Set.
        mnuControler.Visible = False                    ' Close Menu Controler Until Select a Normal USB Number.
        nmuTanks.Visible = False                        ' Close Menu Tanks Until Select a Normal USB Number.
    End Sub

    Private Sub mnuSelect_Click(sender As System.Object, e As System.EventArgs) Handles mnuSelect.Click
        frmAction.tmrExtra.Enabled = False
        frmAction.tmrAutoRun.Enabled = False
        frmAction.Hide()                                ' Hide Form Action
        frmTanks.Hide()                                 ' Hide Form Tanks
        frmMyFTDI.MdiParent = Me
        frmMyFTDI.Show()                                ' Show Form FTDI
        Call frmMyFTDI.UpdateUSB_Scan()                 ' Update Form FTDI.
    End Sub

    Private Sub mnuControler_Click(sender As System.Object, e As System.EventArgs) Handles mnuControler.Click
        frmAction.tmrExtra.Enabled = False
        frmAction.tmrAutoRun.Enabled = False
        TimeStart = DateTime.Now                        ' Get Current Time Date
        frmMyFTDI.Hide()                                ' Hide Form FTDI
        frmTanks.Hide()                                 ' Hide Form Tanks
        frmAction.MdiParent = Me
        frmAction.Show()                                ' Show Form Action
        Call frmAction.UpdateAction()                   ' Update Form Action.
    End Sub

    Private Sub nmuTanks_Click(sender As System.Object, e As System.EventArgs) Handles nmuTanks.Click
        frmAction.tmrExtra.Enabled = False
        frmAction.tmrAutoRun.Enabled = False
        frmAction.Hide()                                ' Hide Form Action
        frmMyFTDI.Hide()                                ' Hide Form FTDI
        frmTanks.MdiParent = Me
        frmTanks.Show()                                 ' Show Form Tanks
        frmTanks.UpdateSettings()                       ' Update Form Tanks.
    End Sub

End Class