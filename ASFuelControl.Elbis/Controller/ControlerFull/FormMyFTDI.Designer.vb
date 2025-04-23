<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmMyFTDI
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
        Me.gridUSB_Scan = New System.Windows.Forms.DataGridView()
        Me.A0 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Description = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Serial_Number = New System.Windows.Forms.DataGridViewTextBoxColumn()
        CType(Me.gridUSB_Scan, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'gridUSB_Scan
        '
        Me.gridUSB_Scan.AllowUserToAddRows = False
        Me.gridUSB_Scan.AllowUserToOrderColumns = True
        Me.gridUSB_Scan.BackgroundColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(192, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.gridUSB_Scan.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.gridUSB_Scan.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.A0, Me.Description, Me.Serial_Number})
        Me.gridUSB_Scan.GridColor = System.Drawing.SystemColors.ControlText
        Me.gridUSB_Scan.Location = New System.Drawing.Point(12, 44)
        Me.gridUSB_Scan.Name = "gridUSB_Scan"
        Me.gridUSB_Scan.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.[Single]
        Me.gridUSB_Scan.RowHeadersWidth = 35
        Me.gridUSB_Scan.Size = New System.Drawing.Size(285, 92)
        Me.gridUSB_Scan.TabIndex = 0
        '
        'A0
        '
        Me.A0.Frozen = True
        Me.A0.HeaderText = "A/A"
        Me.A0.Name = "A0"
        Me.A0.ReadOnly = True
        Me.A0.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.A0.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.A0.Width = 30
        '
        'Description
        '
        Me.Description.Frozen = True
        Me.Description.HeaderText = "Description"
        Me.Description.Name = "Description"
        Me.Description.ReadOnly = True
        Me.Description.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Description.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        '
        'Serial_Number
        '
        Me.Serial_Number.Frozen = True
        Me.Serial_Number.HeaderText = "Serial Number"
        Me.Serial_Number.Name = "Serial_Number"
        Me.Serial_Number.ReadOnly = True
        Me.Serial_Number.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Serial_Number.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        '
        'frmMyFTDI
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(981, 612)
        Me.Controls.Add(Me.gridUSB_Scan)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.Name = "frmMyFTDI"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "FTDI Settings and Properties"
        CType(Me.gridUSB_Scan, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents gridUSB_Scan As System.Windows.Forms.DataGridView
    Friend WithEvents A0 As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Description As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Serial_Number As System.Windows.Forms.DataGridViewTextBoxColumn
End Class
