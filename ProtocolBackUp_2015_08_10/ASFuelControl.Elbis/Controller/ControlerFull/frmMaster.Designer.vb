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
        Me.DateTimePicker1 = New System.Windows.Forms.DateTimePicker()
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip()
        Me.ToolStrip_Select_USB = New System.Windows.Forms.ToolStripButton()
        Me.ToolStrip_Controler = New System.Windows.Forms.ToolStripButton()
        Me.ToolStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'DateTimePicker1
        '
        Me.DateTimePicker1.Location = New System.Drawing.Point(758, 3)
        Me.DateTimePicker1.Name = "DateTimePicker1"
        Me.DateTimePicker1.Size = New System.Drawing.Size(230, 20)
        Me.DateTimePicker1.TabIndex = 4
        '
        'ToolStrip1
        '
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStrip_Select_USB, Me.ToolStrip_Controler})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 0)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Size = New System.Drawing.Size(1508, 25)
        Me.ToolStrip1.TabIndex = 6
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'ToolStrip_Select_USB
        '
        Me.ToolStrip_Select_USB.Image = Global.ControlerFull.My.Resources.Resources.Chip
        Me.ToolStrip_Select_USB.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStrip_Select_USB.Name = "ToolStrip_Select_USB"
        Me.ToolStrip_Select_USB.Size = New System.Drawing.Size(78, 22)
        Me.ToolStrip_Select_USB.Text = "Select USB"
        '
        'ToolStrip_Controler
        '
        Me.ToolStrip_Controler.Image = Global.ControlerFull.My.Resources.Resources.GASPUMP
        Me.ToolStrip_Controler.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStrip_Controler.Name = "ToolStrip_Controler"
        Me.ToolStrip_Controler.Size = New System.Drawing.Size(72, 22)
        Me.ToolStrip_Controler.Text = "Controler"
        '
        'frmMaster
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.ClientSize = New System.Drawing.Size(1508, 704)
        Me.Controls.Add(Me.DateTimePicker1)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.IsMdiContainer = True
        Me.Name = "frmMaster"
        Me.Text = "Master"
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents DateTimePicker1 As System.Windows.Forms.DateTimePicker
    Friend WithEvents ToolStrip1 As System.Windows.Forms.ToolStrip
    Friend WithEvents ToolStrip_Select_USB As System.Windows.Forms.ToolStripButton
    Friend WithEvents ToolStrip_Controler As System.Windows.Forms.ToolStripButton

End Class
