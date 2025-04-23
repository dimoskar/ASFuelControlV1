<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmMaster
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmMaster))
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.mnuSelect = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuControler = New System.Windows.Forms.ToolStripMenuItem()
        Me.nmuTanks = New System.Windows.Forms.ToolStripMenuItem()
        Me.MenuStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'MenuStrip1
        '
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuSelect, Me.mnuControler, Me.nmuTanks})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(992, 24)
        Me.MenuStrip1.TabIndex = 1
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'mnuSelect
        '
        Me.mnuSelect.Name = "mnuSelect"
        Me.mnuSelect.Size = New System.Drawing.Size(70, 20)
        Me.mnuSelect.Text = "Select USB"
        '
        'mnuControler
        '
        Me.mnuControler.Name = "mnuControler"
        Me.mnuControler.Size = New System.Drawing.Size(64, 20)
        Me.mnuControler.Text = "Controler"
        '
        'nmuTanks
        '
        Me.nmuTanks.Name = "nmuTanks"
        Me.nmuTanks.Size = New System.Drawing.Size(47, 20)
        Me.nmuTanks.Text = "Tanks"
        '
        'frmMaster
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(992, 666)
        Me.Controls.Add(Me.MenuStrip1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.IsMdiContainer = True
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Name = "frmMaster"
        Me.Text = "Master"
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents MenuStrip1 As System.Windows.Forms.MenuStrip
    Friend WithEvents mnuSelect As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuControler As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents nmuTanks As System.Windows.Forms.ToolStripMenuItem
End Class
