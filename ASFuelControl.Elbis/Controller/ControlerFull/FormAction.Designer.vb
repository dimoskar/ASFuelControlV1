<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmAction
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
        Me.components = New System.ComponentModel.Container()
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Me.gridAction = New System.Windows.Forms.DataGridView()
        Me.Chan = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.NzNz = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Points = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.PStat = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.PErr = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.APStat = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Master = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Alive = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Block = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Auth = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.GetTra = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.NeedA = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.OnFill = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.NzOut = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.DPrice = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.DAmou = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.DVolu = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Price = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.CAmou = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.CVolu = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.TAmou = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.TVolu = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Pomp = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Iden = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Nozz = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Port = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Styl = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Sped = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Data = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Prod = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.txtExplane = New System.Windows.Forms.TextBox()
        Me.txtHexTransparent = New System.Windows.Forms.TextBox()
        Me.cmdAutoRun = New System.Windows.Forms.Button()
        Me.tmrAutoRun = New System.Windows.Forms.Timer(Me.components)
        Me.txtActionTimes = New System.Windows.Forms.TextBox()
        Me.cmbDeviceModel = New System.Windows.Forms.ComboBox()
        Me.cmbIdentity = New System.Windows.Forms.ComboBox()
        Me.cmbCommPort = New System.Windows.Forms.ComboBox()
        Me.cmdStoreSets = New System.Windows.Forms.Button()
        Me.cmbCommSpeed = New System.Windows.Forms.ComboBox()
        Me.cmbCommStyle = New System.Windows.Forms.ComboBox()
        Me.txtElapseSeconds = New System.Windows.Forms.TextBox()
        Me.cmbCommDatas = New System.Windows.Forms.ComboBox()
        Me.txtOutTransparent = New System.Windows.Forms.TextBox()
        Me.lblChannel = New System.Windows.Forms.Label()
        Me.cmdSendTrans = New System.Windows.Forms.Button()
        Me.tmrExtra = New System.Windows.Forms.Timer(Me.components)
        Me.lblWorkWithChannel = New System.Windows.Forms.Label()
        Me.cmdClearList = New System.Windows.Forms.Button()
        Me.cmbProducts = New System.Windows.Forms.ComboBox()
        Me.btnRestart = New System.Windows.Forms.Button()
        Me.cmdChangePrice = New System.Windows.Forms.Button()
        Me.txtChangePrice = New System.Windows.Forms.TextBox()
        Me.txtPresetVolume = New System.Windows.Forms.TextBox()
        Me.cmdPresetVolume = New System.Windows.Forms.Button()
        Me.txtPresetAmount = New System.Windows.Forms.TextBox()
        Me.cmdPresetAmount = New System.Windows.Forms.Button()
        Me.txtIdentity = New System.Windows.Forms.TextBox()
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.cmbTotalNozzles = New System.Windows.Forms.ComboBox()
        Me.btnAskSettings = New System.Windows.Forms.Button()
        Me.btnAskDeliveries = New System.Windows.Forms.Button()
        Me.btnGetTotals = New System.Windows.Forms.Button()
        Me.txtAsciiTransparent = New System.Windows.Forms.TextBox()
        Me.txtBlock = New System.Windows.Forms.TextBox()
        Me.btnAskSettingsUsed = New System.Windows.Forms.Button()
        Me.btnAskDeliveriesUsed = New System.Windows.Forms.Button()
        Me.btnGetTotalsUsed = New System.Windows.Forms.Button()
        Me.txtStatusEvent = New System.Windows.Forms.TextBox()
        Me.txtPressetsEvent = New System.Windows.Forms.TextBox()
        Me.txtDeliveryEvent = New System.Windows.Forms.TextBox()
        Me.txtTotalsEvent = New System.Windows.Forms.TextBox()
        Me.lboxInformations = New System.Windows.Forms.ListBox()
        Me.lblRunInfo = New System.Windows.Forms.Label()
        Me.txtReserved2 = New System.Windows.Forms.TextBox()
        Me.cmdReserved2 = New System.Windows.Forms.Button()
        Me.txtReserved1 = New System.Windows.Forms.TextBox()
        Me.cmdReserved1 = New System.Windows.Forms.Button()
        Me.txtSerialNumber = New System.Windows.Forms.TextBox()
        Me.cmdSerialNumber = New System.Windows.Forms.Button()
        Me.txtDescription = New System.Windows.Forms.TextBox()
        Me.cmdDescription = New System.Windows.Forms.Button()
        Me.btnGetController = New System.Windows.Forms.Button()
        Me.gridUSB_Scan = New System.Windows.Forms.DataGridView()
        Me.AA = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Description = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Serial_Number = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.btnBlock = New System.Windows.Forms.Button()
        Me.btnResume = New System.Windows.Forms.Button()
        CType(Me.gridAction, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridUSB_Scan, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'gridAction
        '
        Me.gridAction.AllowUserToAddRows = False
        Me.gridAction.AllowUserToResizeColumns = False
        Me.gridAction.AllowUserToResizeRows = False
        Me.gridAction.BackgroundColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(192, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.gridAction.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.Chan, Me.NzNz, Me.Points, Me.PStat, Me.PErr, Me.APStat, Me.Master, Me.Alive, Me.Block, Me.Auth, Me.GetTra, Me.NeedA, Me.OnFill, Me.NzOut, Me.DPrice, Me.DAmou, Me.DVolu, Me.Price, Me.CAmou, Me.CVolu, Me.TAmou, Me.TVolu, Me.Pomp, Me.Iden, Me.Nozz, Me.Port, Me.Styl, Me.Sped, Me.Data, Me.Prod})
        DataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight
        DataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window
        DataGridViewCellStyle1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(161, Byte))
        DataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText
        DataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
        Me.gridAction.DefaultCellStyle = DataGridViewCellStyle1
        Me.gridAction.GridColor = System.Drawing.Color.Black
        Me.gridAction.Location = New System.Drawing.Point(0, 132)
        Me.gridAction.Name = "gridAction"
        Me.gridAction.ReadOnly = True
        Me.gridAction.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.[Single]
        Me.gridAction.RowHeadersVisible = False
        Me.gridAction.RowHeadersWidth = 35
        Me.gridAction.RowTemplate.Height = 20
        Me.gridAction.Size = New System.Drawing.Size(1508, 525)
        Me.gridAction.TabIndex = 1
        '
        'Chan
        '
        Me.Chan.Frozen = True
        Me.Chan.HeaderText = "CH."
        Me.Chan.Name = "Chan"
        Me.Chan.ReadOnly = True
        Me.Chan.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Chan.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Chan.ToolTipText = "Channel"
        Me.Chan.Width = 30
        '
        'NzNz
        '
        Me.NzNz.Frozen = True
        Me.NzNz.HeaderText = "N/N"
        Me.NzNz.Name = "NzNz"
        Me.NzNz.ReadOnly = True
        Me.NzNz.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.NzNz.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.NzNz.ToolTipText = "Total Nozzles/Last Nozzle Up"
        Me.NzNz.Width = 30
        '
        'Points
        '
        Me.Points.FillWeight = 50.0!
        Me.Points.Frozen = True
        Me.Points.HeaderText = "DecP"
        Me.Points.Name = "Points"
        Me.Points.ReadOnly = True
        Me.Points.ToolTipText = "Decimal Points on Action Point."
        Me.Points.Width = 40
        '
        'PStat
        '
        Me.PStat.FillWeight = 50.0!
        Me.PStat.Frozen = True
        Me.PStat.HeaderText = "PST"
        Me.PStat.Name = "PStat"
        Me.PStat.ReadOnly = True
        Me.PStat.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.PStat.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.PStat.ToolTipText = "Original Pump Status"
        Me.PStat.Width = 30
        '
        'PErr
        '
        Me.PErr.Frozen = True
        Me.PErr.HeaderText = "PER"
        Me.PErr.Name = "PErr"
        Me.PErr.ReadOnly = True
        Me.PErr.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.PErr.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.PErr.ToolTipText = "Last Pump Error."
        Me.PErr.Width = 30
        '
        'APStat
        '
        Me.APStat.Frozen = True
        Me.APStat.HeaderText = "APS"
        Me.APStat.Name = "APStat"
        Me.APStat.ReadOnly = True
        Me.APStat.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.APStat.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.APStat.ToolTipText = "Action Point Status."
        Me.APStat.Width = 30
        '
        'Master
        '
        Me.Master.Frozen = True
        Me.Master.HeaderText = "M/S"
        Me.Master.Name = "Master"
        Me.Master.ReadOnly = True
        Me.Master.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Master.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Master.ToolTipText = "Indicate if This Channel Is Master."
        Me.Master.Width = 36
        '
        'Alive
        '
        Me.Alive.Frozen = True
        Me.Alive.HeaderText = "Alive"
        Me.Alive.Name = "Alive"
        Me.Alive.ReadOnly = True
        Me.Alive.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Alive.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Alive.ToolTipText = "Indicate is This Channel is Alive."
        Me.Alive.Width = 36
        '
        'Block
        '
        Me.Block.Frozen = True
        Me.Block.HeaderText = "Block"
        Me.Block.Name = "Block"
        Me.Block.ReadOnly = True
        Me.Block.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Block.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Block.ToolTipText = "Indicate is This Channel is Blocked."
        Me.Block.Width = 36
        '
        'Auth
        '
        Me.Auth.Frozen = True
        Me.Auth.HeaderText = "Auth."
        Me.Auth.Name = "Auth"
        Me.Auth.ReadOnly = True
        Me.Auth.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Auth.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Auth.ToolTipText = "Indicate is This Channel Authorized."
        Me.Auth.Width = 36
        '
        'GetTra
        '
        Me.GetTra.Frozen = True
        Me.GetTra.HeaderText = "GetT."
        Me.GetTra.Name = "GetTra"
        Me.GetTra.ReadOnly = True
        Me.GetTra.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.GetTra.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.GetTra.ToolTipText = "Indicate is This Channel Say Transaction Finish and Data Ready."
        Me.GetTra.Width = 36
        '
        'NeedA
        '
        Me.NeedA.Frozen = True
        Me.NeedA.HeaderText = "Need"
        Me.NeedA.Name = "NeedA"
        Me.NeedA.ReadOnly = True
        Me.NeedA.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.NeedA.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.NeedA.ToolTipText = "Indicate is This Channel Need Authorize."
        Me.NeedA.Width = 36
        '
        'OnFill
        '
        Me.OnFill.Frozen = True
        Me.OnFill.HeaderText = "Filling"
        Me.OnFill.Name = "OnFill"
        Me.OnFill.ReadOnly = True
        Me.OnFill.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.OnFill.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.OnFill.ToolTipText = "Indicate is This Channel Filling Now."
        Me.OnFill.Width = 36
        '
        'NzOut
        '
        Me.NzOut.Frozen = True
        Me.NzOut.HeaderText = "NOut"
        Me.NzOut.Name = "NzOut"
        Me.NzOut.ReadOnly = True
        Me.NzOut.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.NzOut.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.NzOut.ToolTipText = "Indicate at This Channel One Nozzle is Out."
        Me.NzOut.Width = 36
        '
        'DPrice
        '
        Me.DPrice.Frozen = True
        Me.DPrice.HeaderText = "Dis.Price"
        Me.DPrice.Name = "DPrice"
        Me.DPrice.ReadOnly = True
        Me.DPrice.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.DPrice.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.DPrice.ToolTipText = "Unit Price to Display."
        Me.DPrice.Width = 60
        '
        'DAmou
        '
        Me.DAmou.Frozen = True
        Me.DAmou.HeaderText = "Dis.Amount"
        Me.DAmou.Name = "DAmou"
        Me.DAmou.ReadOnly = True
        Me.DAmou.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.DAmou.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.DAmou.ToolTipText = "Filling Amount to Display."
        Me.DAmou.Width = 70
        '
        'DVolu
        '
        Me.DVolu.Frozen = True
        Me.DVolu.HeaderText = "Dis.Volume"
        Me.DVolu.Name = "DVolu"
        Me.DVolu.ReadOnly = True
        Me.DVolu.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.DVolu.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.DVolu.ToolTipText = "Filling Volume to Display."
        Me.DVolu.Width = 70
        '
        'Price
        '
        Me.Price.Frozen = True
        Me.Price.HeaderText = "Cur.Price"
        Me.Price.Name = "Price"
        Me.Price.ReadOnly = True
        Me.Price.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Price.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Price.ToolTipText = "Current Unit Price."
        Me.Price.Width = 60
        '
        'CAmou
        '
        Me.CAmou.Frozen = True
        Me.CAmou.HeaderText = "Cur.Amount"
        Me.CAmou.Name = "CAmou"
        Me.CAmou.ReadOnly = True
        Me.CAmou.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.CAmou.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.CAmou.ToolTipText = "Current Amount"
        Me.CAmou.Width = 70
        '
        'CVolu
        '
        Me.CVolu.Frozen = True
        Me.CVolu.HeaderText = "Cur. Volume"
        Me.CVolu.Name = "CVolu"
        Me.CVolu.ReadOnly = True
        Me.CVolu.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.CVolu.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.CVolu.ToolTipText = "Current Volume"
        Me.CVolu.Width = 70
        '
        'TAmou
        '
        Me.TAmou.FillWeight = 80.0!
        Me.TAmou.Frozen = True
        Me.TAmou.HeaderText = "Total Amount"
        Me.TAmou.Name = "TAmou"
        Me.TAmou.ReadOnly = True
        Me.TAmou.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.TAmou.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.TAmou.ToolTipText = "Total Amount"
        Me.TAmou.Width = 80
        '
        'TVolu
        '
        Me.TVolu.FillWeight = 80.0!
        Me.TVolu.Frozen = True
        Me.TVolu.HeaderText = "Total Volume"
        Me.TVolu.Name = "TVolu"
        Me.TVolu.ReadOnly = True
        Me.TVolu.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.TVolu.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.TVolu.ToolTipText = "Total Volume"
        Me.TVolu.Width = 80
        '
        'Pomp
        '
        Me.Pomp.Frozen = True
        Me.Pomp.HeaderText = "Pomp Model"
        Me.Pomp.Name = "Pomp"
        Me.Pomp.ReadOnly = True
        Me.Pomp.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Pomp.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Pomp.ToolTipText = "Selected Pomp Model"
        Me.Pomp.Width = 80
        '
        'Iden
        '
        Me.Iden.Frozen = True
        Me.Iden.HeaderText = "Identity"
        Me.Iden.Name = "Iden"
        Me.Iden.ReadOnly = True
        Me.Iden.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Iden.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Iden.ToolTipText = "Selected Identity"
        Me.Iden.Width = 50
        '
        'Nozz
        '
        Me.Nozz.Frozen = True
        Me.Nozz.HeaderText = "Nozzle"
        Me.Nozz.Name = "Nozz"
        Me.Nozz.ReadOnly = True
        Me.Nozz.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Nozz.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Nozz.ToolTipText = "Selected Nozzle on Device."
        Me.Nozz.Width = 50
        '
        'Port
        '
        Me.Port.Frozen = True
        Me.Port.HeaderText = "Port"
        Me.Port.Name = "Port"
        Me.Port.ReadOnly = True
        Me.Port.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Port.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Port.ToolTipText = "Port On Controller."
        Me.Port.Width = 50
        '
        'Styl
        '
        Me.Styl.Frozen = True
        Me.Styl.HeaderText = "Style"
        Me.Styl.Name = "Styl"
        Me.Styl.ReadOnly = True
        Me.Styl.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Styl.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Styl.ToolTipText = "Type of Communication"
        Me.Styl.Width = 50
        '
        'Sped
        '
        Me.Sped.Frozen = True
        Me.Sped.HeaderText = "Speed"
        Me.Sped.Name = "Sped"
        Me.Sped.ReadOnly = True
        Me.Sped.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Sped.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Sped.ToolTipText = "Communication Speed"
        Me.Sped.Width = 50
        '
        'Data
        '
        Me.Data.Frozen = True
        Me.Data.HeaderText = "Data"
        Me.Data.Name = "Data"
        Me.Data.ReadOnly = True
        Me.Data.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Data.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Data.ToolTipText = "Communication Data Style."
        Me.Data.Width = 50
        '
        'Prod
        '
        Me.Prod.Frozen = True
        Me.Prod.HeaderText = "Product"
        Me.Prod.Name = "Prod"
        Me.Prod.ReadOnly = True
        Me.Prod.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Prod.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Prod.ToolTipText = "Product on This Channel."
        Me.Prod.Width = 70
        '
        'txtExplane
        '
        Me.txtExplane.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtExplane.Location = New System.Drawing.Point(408, 85)
        Me.txtExplane.Name = "txtExplane"
        Me.txtExplane.Size = New System.Drawing.Size(253, 20)
        Me.txtExplane.TabIndex = 2
        '
        'txtHexTransparent
        '
        Me.txtHexTransparent.BackColor = System.Drawing.Color.White
        Me.txtHexTransparent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtHexTransparent.Location = New System.Drawing.Point(408, 32)
        Me.txtHexTransparent.Name = "txtHexTransparent"
        Me.txtHexTransparent.ReadOnly = True
        Me.txtHexTransparent.Size = New System.Drawing.Size(778, 20)
        Me.txtHexTransparent.TabIndex = 6
        Me.ToolTip1.SetToolTip(Me.txtHexTransparent, "Here is the Transparent Packet on Hex Format." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "(For Looking Only.)" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10))
        '
        'cmdAutoRun
        '
        Me.cmdAutoRun.BackColor = System.Drawing.Color.Lime
        Me.cmdAutoRun.Font = New System.Drawing.Font("Courier New", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(161, Byte))
        Me.cmdAutoRun.Image = Global.ControlerFull.My.Resources.Resources.media_play_green
        Me.cmdAutoRun.Location = New System.Drawing.Point(895, 85)
        Me.cmdAutoRun.Name = "cmdAutoRun"
        Me.cmdAutoRun.Size = New System.Drawing.Size(65, 45)
        Me.cmdAutoRun.TabIndex = 10
        Me.cmdAutoRun.Text = "RUN"
        Me.cmdAutoRun.UseVisualStyleBackColor = False
        '
        'tmrAutoRun
        '
        '
        'txtActionTimes
        '
        Me.txtActionTimes.Location = New System.Drawing.Point(486, 109)
        Me.txtActionTimes.Name = "txtActionTimes"
        Me.txtActionTimes.Size = New System.Drawing.Size(72, 20)
        Me.txtActionTimes.TabIndex = 11
        Me.txtActionTimes.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        Me.ToolTip1.SetToolTip(Me.txtActionTimes, "Communication Error Times / Action Times.")
        '
        'cmbDeviceModel
        '
        Me.cmbDeviceModel.FormattingEnabled = True
        Me.cmbDeviceModel.Location = New System.Drawing.Point(1038, 109)
        Me.cmbDeviceModel.Name = "cmbDeviceModel"
        Me.cmbDeviceModel.Size = New System.Drawing.Size(78, 21)
        Me.cmbDeviceModel.TabIndex = 13
        Me.ToolTip1.SetToolTip(Me.cmbDeviceModel, "Select Action Point.")
        '
        'cmbIdentity
        '
        Me.cmbIdentity.FormattingEnabled = True
        Me.cmbIdentity.Location = New System.Drawing.Point(1119, 109)
        Me.cmbIdentity.Name = "cmbIdentity"
        Me.cmbIdentity.Size = New System.Drawing.Size(48, 21)
        Me.cmbIdentity.TabIndex = 14
        Me.ToolTip1.SetToolTip(Me.cmbIdentity, "Select Identity for this Action Point.")
        '
        'cmbCommPort
        '
        Me.cmbCommPort.FormattingEnabled = True
        Me.cmbCommPort.Location = New System.Drawing.Point(1218, 109)
        Me.cmbCommPort.Name = "cmbCommPort"
        Me.cmbCommPort.Size = New System.Drawing.Size(48, 21)
        Me.cmbCommPort.TabIndex = 16
        Me.ToolTip1.SetToolTip(Me.cmbCommPort, "Select Communication Port on Controller Device.")
        '
        'cmdStoreSets
        '
        Me.cmdStoreSets.BackColor = System.Drawing.Color.Aqua
        Me.cmdStoreSets.ForeColor = System.Drawing.Color.Black
        Me.cmdStoreSets.Location = New System.Drawing.Point(966, 85)
        Me.cmdStoreSets.Name = "cmdStoreSets"
        Me.cmdStoreSets.Size = New System.Drawing.Size(66, 44)
        Me.cmdStoreSets.TabIndex = 19
        Me.cmdStoreSets.Text = "Store Settings"
        Me.ToolTip1.SetToolTip(Me.cmdStoreSets, "Store Presseting Data to Selected Channel.")
        Me.cmdStoreSets.UseVisualStyleBackColor = False
        '
        'cmbCommSpeed
        '
        Me.cmbCommSpeed.FormattingEnabled = True
        Me.cmbCommSpeed.Items.AddRange(New Object() {"Auto", "1200", "2400", "4800", "5787", "9600", "19200"})
        Me.cmbCommSpeed.Location = New System.Drawing.Point(1318, 109)
        Me.cmbCommSpeed.Name = "cmbCommSpeed"
        Me.cmbCommSpeed.Size = New System.Drawing.Size(48, 21)
        Me.cmbCommSpeed.TabIndex = 20
        Me.ToolTip1.SetToolTip(Me.cmbCommSpeed, "Select Communication Speed for the Selected Channel.")
        '
        'cmbCommStyle
        '
        Me.cmbCommStyle.FormattingEnabled = True
        Me.cmbCommStyle.Location = New System.Drawing.Point(1268, 109)
        Me.cmbCommStyle.Name = "cmbCommStyle"
        Me.cmbCommStyle.Size = New System.Drawing.Size(48, 21)
        Me.cmbCommStyle.TabIndex = 21
        Me.ToolTip1.SetToolTip(Me.cmbCommStyle, "Select Communication Style of Communication Port.")
        '
        'txtElapseSeconds
        '
        Me.txtElapseSeconds.Location = New System.Drawing.Point(408, 109)
        Me.txtElapseSeconds.Name = "txtElapseSeconds"
        Me.txtElapseSeconds.Size = New System.Drawing.Size(72, 20)
        Me.txtElapseSeconds.TabIndex = 22
        Me.txtElapseSeconds.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        Me.ToolTip1.SetToolTip(Me.txtElapseSeconds, "Communication Elapsed Time.")
        Me.txtElapseSeconds.WordWrap = False
        '
        'cmbCommDatas
        '
        Me.cmbCommDatas.FormattingEnabled = True
        Me.cmbCommDatas.Items.AddRange(New Object() {"Auto", "8,n,1", "8,e,1", "8,o,1", "7,n,1", "7,e,1", "7,o,1", "8,n,2", "8,e,2", "8,o,2", "7,n,2", "7,e,2", "7,o,2"})
        Me.cmbCommDatas.Location = New System.Drawing.Point(1368, 109)
        Me.cmbCommDatas.Name = "cmbCommDatas"
        Me.cmbCommDatas.Size = New System.Drawing.Size(48, 21)
        Me.cmbCommDatas.TabIndex = 31
        Me.ToolTip1.SetToolTip(Me.cmbCommDatas, "Select Communication Data Length, Parity and Stop Bits for the Selected Channel.")
        '
        'txtOutTransparent
        '
        Me.txtOutTransparent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtOutTransparent.Location = New System.Drawing.Point(408, 6)
        Me.txtOutTransparent.Name = "txtOutTransparent"
        Me.txtOutTransparent.Size = New System.Drawing.Size(778, 20)
        Me.txtOutTransparent.TabIndex = 39
        Me.txtOutTransparent.Text = "&M00003, $0D, $0A"
        Me.ToolTip1.SetToolTip(Me.txtOutTransparent, "Type Here a String to Send Transparent at Selected Channel" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Type &xxxx  for Strin" & _
        "g" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Type $xx  for Hex Value" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Example : &M00003, $0D, $0A" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10))
        '
        'lblChannel
        '
        Me.lblChannel.AutoSize = True
        Me.lblChannel.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(161, Byte))
        Me.lblChannel.ForeColor = System.Drawing.Color.Yellow
        Me.lblChannel.Location = New System.Drawing.Point(180, 65)
        Me.lblChannel.Name = "lblChannel"
        Me.lblChannel.Size = New System.Drawing.Size(53, 13)
        Me.lblChannel.TabIndex = 42
        Me.lblChannel.Text = "Channel"
        '
        'cmdSendTrans
        '
        Me.cmdSendTrans.Location = New System.Drawing.Point(323, 87)
        Me.cmdSendTrans.Name = "cmdSendTrans"
        Me.cmdSendTrans.Size = New System.Drawing.Size(79, 42)
        Me.cmdSendTrans.TabIndex = 44
        Me.cmdSendTrans.Text = "Send Transparent"
        Me.ToolTip1.SetToolTip(Me.cmdSendTrans, "Send Data to Controler " & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & " at Selected Channel," & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "on Transparent Mode.")
        Me.cmdSendTrans.UseVisualStyleBackColor = True
        '
        'tmrExtra
        '
        Me.tmrExtra.Interval = 500
        '
        'lblWorkWithChannel
        '
        Me.lblWorkWithChannel.Font = New System.Drawing.Font("Microsoft Sans Serif", 24.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblWorkWithChannel.ForeColor = System.Drawing.Color.Yellow
        Me.lblWorkWithChannel.Location = New System.Drawing.Point(176, 83)
        Me.lblWorkWithChannel.Name = "lblWorkWithChannel"
        Me.lblWorkWithChannel.Size = New System.Drawing.Size(60, 44)
        Me.lblWorkWithChannel.TabIndex = 45
        Me.lblWorkWithChannel.Text = "24"
        Me.lblWorkWithChannel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'cmdClearList
        '
        Me.cmdClearList.Location = New System.Drawing.Point(1059, 83)
        Me.cmdClearList.Name = "cmdClearList"
        Me.cmdClearList.Size = New System.Drawing.Size(127, 22)
        Me.cmdClearList.TabIndex = 46
        Me.cmdClearList.Text = "Clear/Refresh Screen"
        Me.ToolTip1.SetToolTip(Me.cmdClearList, "Clear All Data, On Data Grid View" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "          and Refresh All Again.")
        Me.cmdClearList.UseVisualStyleBackColor = True
        '
        'cmbProducts
        '
        Me.cmbProducts.FormattingEnabled = True
        Me.cmbProducts.Items.AddRange(New Object() {"1", "2", "3", "4", "5", "6", "7", "8", "9", "10"})
        Me.cmbProducts.Location = New System.Drawing.Point(1418, 109)
        Me.cmbProducts.Name = "cmbProducts"
        Me.cmbProducts.Size = New System.Drawing.Size(69, 21)
        Me.cmbProducts.TabIndex = 47
        Me.ToolTip1.SetToolTip(Me.cmbProducts, "Select Product On Nozzle.")
        '
        'btnRestart
        '
        Me.btnRestart.Location = New System.Drawing.Point(242, 88)
        Me.btnRestart.Name = "btnRestart"
        Me.btnRestart.Size = New System.Drawing.Size(75, 22)
        Me.btnRestart.TabIndex = 48
        Me.btnRestart.Text = "Reset"
        Me.ToolTip1.SetToolTip(Me.btnRestart, "Restart Controler." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Please Wait for Collect All Data." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10))
        Me.btnRestart.UseVisualStyleBackColor = True
        '
        'cmdChangePrice
        '
        Me.cmdChangePrice.Location = New System.Drawing.Point(667, 107)
        Me.cmdChangePrice.Name = "cmdChangePrice"
        Me.cmdChangePrice.Size = New System.Drawing.Size(66, 22)
        Me.cmdChangePrice.TabIndex = 50
        Me.cmdChangePrice.Text = "Set Price"
        Me.ToolTip1.SetToolTip(Me.cmdChangePrice, "Click This Button to Change Unit Price")
        Me.cmdChangePrice.UseVisualStyleBackColor = True
        '
        'txtChangePrice
        '
        Me.txtChangePrice.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtChangePrice.Location = New System.Drawing.Point(667, 85)
        Me.txtChangePrice.Name = "txtChangePrice"
        Me.txtChangePrice.Size = New System.Drawing.Size(66, 20)
        Me.txtChangePrice.TabIndex = 51
        Me.txtChangePrice.Text = "0"
        Me.txtChangePrice.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        Me.ToolTip1.SetToolTip(Me.txtChangePrice, "Set Here New Unit Price and Push The Button")
        '
        'txtPresetVolume
        '
        Me.txtPresetVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtPresetVolume.Location = New System.Drawing.Point(739, 85)
        Me.txtPresetVolume.Name = "txtPresetVolume"
        Me.txtPresetVolume.Size = New System.Drawing.Size(66, 20)
        Me.txtPresetVolume.TabIndex = 53
        Me.txtPresetVolume.Text = "0"
        Me.txtPresetVolume.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        Me.ToolTip1.SetToolTip(Me.txtPresetVolume, "Set Here Preset Volume and Push The Button")
        '
        'cmdPresetVolume
        '
        Me.cmdPresetVolume.Location = New System.Drawing.Point(739, 107)
        Me.cmdPresetVolume.Name = "cmdPresetVolume"
        Me.cmdPresetVolume.Size = New System.Drawing.Size(66, 22)
        Me.cmdPresetVolume.TabIndex = 52
        Me.cmdPresetVolume.Text = "Volume"
        Me.ToolTip1.SetToolTip(Me.cmdPresetVolume, "Click This Button to Send Preset Volume")
        Me.cmdPresetVolume.UseVisualStyleBackColor = True
        '
        'txtPresetAmount
        '
        Me.txtPresetAmount.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtPresetAmount.Location = New System.Drawing.Point(811, 85)
        Me.txtPresetAmount.Name = "txtPresetAmount"
        Me.txtPresetAmount.Size = New System.Drawing.Size(66, 20)
        Me.txtPresetAmount.TabIndex = 55
        Me.txtPresetAmount.Text = "0"
        Me.txtPresetAmount.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        Me.ToolTip1.SetToolTip(Me.txtPresetAmount, "Set Here Preset Amount and Push The Button")
        '
        'cmdPresetAmount
        '
        Me.cmdPresetAmount.Location = New System.Drawing.Point(811, 107)
        Me.cmdPresetAmount.Name = "cmdPresetAmount"
        Me.cmdPresetAmount.Size = New System.Drawing.Size(66, 22)
        Me.cmdPresetAmount.TabIndex = 54
        Me.cmdPresetAmount.Text = "Amount"
        Me.ToolTip1.SetToolTip(Me.cmdPresetAmount, "Click This Button to Send Preset Amount.")
        Me.cmdPresetAmount.UseVisualStyleBackColor = True
        '
        'txtIdentity
        '
        Me.txtIdentity.Location = New System.Drawing.Point(1119, 110)
        Me.txtIdentity.Name = "txtIdentity"
        Me.txtIdentity.Size = New System.Drawing.Size(48, 20)
        Me.txtIdentity.TabIndex = 56
        Me.txtIdentity.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        Me.ToolTip1.SetToolTip(Me.txtIdentity, "Set Identity for the Selected Channel." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Normal Value 0-99999")
        '
        'cmbTotalNozzles
        '
        Me.cmbTotalNozzles.FormattingEnabled = True
        Me.cmbTotalNozzles.Location = New System.Drawing.Point(1168, 109)
        Me.cmbTotalNozzles.Name = "cmbTotalNozzles"
        Me.cmbTotalNozzles.Size = New System.Drawing.Size(48, 21)
        Me.cmbTotalNozzles.TabIndex = 94
        Me.ToolTip1.SetToolTip(Me.cmbTotalNozzles, "Set Total Nozzles on This Action Point.")
        '
        'btnAskSettings
        '
        Me.btnAskSettings.Location = New System.Drawing.Point(242, 5)
        Me.btnAskSettings.Name = "btnAskSettings"
        Me.btnAskSettings.Size = New System.Drawing.Size(75, 22)
        Me.btnAskSettings.TabIndex = 96
        Me.btnAskSettings.Text = "Settings"
        Me.ToolTip1.SetToolTip(Me.btnAskSettings, "Ask All Channels About Pressetings and Status.")
        Me.btnAskSettings.UseVisualStyleBackColor = True
        '
        'btnAskDeliveries
        '
        Me.btnAskDeliveries.Location = New System.Drawing.Point(242, 33)
        Me.btnAskDeliveries.Name = "btnAskDeliveries"
        Me.btnAskDeliveries.Size = New System.Drawing.Size(75, 21)
        Me.btnAskDeliveries.TabIndex = 97
        Me.btnAskDeliveries.Text = "Deliveries"
        Me.ToolTip1.SetToolTip(Me.btnAskDeliveries, "Ask All Channels About Deliveries.")
        Me.btnAskDeliveries.UseVisualStyleBackColor = True
        '
        'btnGetTotals
        '
        Me.btnGetTotals.Location = New System.Drawing.Point(242, 60)
        Me.btnGetTotals.Name = "btnGetTotals"
        Me.btnGetTotals.Size = New System.Drawing.Size(75, 22)
        Me.btnGetTotals.TabIndex = 98
        Me.btnGetTotals.Text = "Get Totals"
        Me.ToolTip1.SetToolTip(Me.btnGetTotals, "Ask All Channels About Totals.")
        Me.btnGetTotals.UseVisualStyleBackColor = True
        '
        'txtAsciiTransparent
        '
        Me.txtAsciiTransparent.BackColor = System.Drawing.Color.White
        Me.txtAsciiTransparent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtAsciiTransparent.Location = New System.Drawing.Point(408, 58)
        Me.txtAsciiTransparent.Name = "txtAsciiTransparent"
        Me.txtAsciiTransparent.ReadOnly = True
        Me.txtAsciiTransparent.Size = New System.Drawing.Size(778, 20)
        Me.txtAsciiTransparent.TabIndex = 99
        Me.ToolTip1.SetToolTip(Me.txtAsciiTransparent, "Here is the Transparent Answer on Ascii Format." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "(For Looking Only.)" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10))
        '
        'txtBlock
        '
        Me.txtBlock.Location = New System.Drawing.Point(1394, 12)
        Me.txtBlock.Name = "txtBlock"
        Me.txtBlock.Size = New System.Drawing.Size(26, 20)
        Me.txtBlock.TabIndex = 105
        Me.txtBlock.Text = "1"
        Me.txtBlock.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        Me.ToolTip1.SetToolTip(Me.txtBlock, "Communication Error Times / Action Times.")
        '
        'btnAskSettingsUsed
        '
        Me.btnAskSettingsUsed.Location = New System.Drawing.Point(323, 5)
        Me.btnAskSettingsUsed.Name = "btnAskSettingsUsed"
        Me.btnAskSettingsUsed.Size = New System.Drawing.Size(75, 22)
        Me.btnAskSettingsUsed.TabIndex = 101
        Me.btnAskSettingsUsed.Text = "Settings"
        Me.ToolTip1.SetToolTip(Me.btnAskSettingsUsed, "Ask Used Channels About Pressetings and Status.")
        Me.btnAskSettingsUsed.UseVisualStyleBackColor = True
        '
        'btnAskDeliveriesUsed
        '
        Me.btnAskDeliveriesUsed.Location = New System.Drawing.Point(323, 34)
        Me.btnAskDeliveriesUsed.Name = "btnAskDeliveriesUsed"
        Me.btnAskDeliveriesUsed.Size = New System.Drawing.Size(75, 20)
        Me.btnAskDeliveriesUsed.TabIndex = 102
        Me.btnAskDeliveriesUsed.Text = "Deliveries"
        Me.ToolTip1.SetToolTip(Me.btnAskDeliveriesUsed, "Ask Used Channels About Deliveries.")
        Me.btnAskDeliveriesUsed.UseVisualStyleBackColor = True
        '
        'btnGetTotalsUsed
        '
        Me.btnGetTotalsUsed.Location = New System.Drawing.Point(323, 60)
        Me.btnGetTotalsUsed.Name = "btnGetTotalsUsed"
        Me.btnGetTotalsUsed.Size = New System.Drawing.Size(75, 23)
        Me.btnGetTotalsUsed.TabIndex = 103
        Me.btnGetTotalsUsed.Text = "Get Totals"
        Me.ToolTip1.SetToolTip(Me.btnGetTotalsUsed, "Ask Used Channels About Totals.")
        Me.btnGetTotalsUsed.UseVisualStyleBackColor = True
        '
        'txtStatusEvent
        '
        Me.txtStatusEvent.Location = New System.Drawing.Point(1192, 83)
        Me.txtStatusEvent.Name = "txtStatusEvent"
        Me.txtStatusEvent.Size = New System.Drawing.Size(25, 20)
        Me.txtStatusEvent.TabIndex = 107
        Me.ToolTip1.SetToolTip(Me.txtStatusEvent, "Status Change Event.")
        '
        'txtPressetsEvent
        '
        Me.txtPressetsEvent.Location = New System.Drawing.Point(1217, 83)
        Me.txtPressetsEvent.Name = "txtPressetsEvent"
        Me.txtPressetsEvent.Size = New System.Drawing.Size(25, 20)
        Me.txtPressetsEvent.TabIndex = 108
        Me.ToolTip1.SetToolTip(Me.txtPressetsEvent, "Settings Change Event.")
        '
        'txtDeliveryEvent
        '
        Me.txtDeliveryEvent.Location = New System.Drawing.Point(1242, 83)
        Me.txtDeliveryEvent.Name = "txtDeliveryEvent"
        Me.txtDeliveryEvent.Size = New System.Drawing.Size(25, 20)
        Me.txtDeliveryEvent.TabIndex = 109
        Me.ToolTip1.SetToolTip(Me.txtDeliveryEvent, "Delivery Change Event.")
        '
        'txtTotalsEvent
        '
        Me.txtTotalsEvent.Location = New System.Drawing.Point(1267, 83)
        Me.txtTotalsEvent.Name = "txtTotalsEvent"
        Me.txtTotalsEvent.Size = New System.Drawing.Size(25, 20)
        Me.txtTotalsEvent.TabIndex = 110
        Me.ToolTip1.SetToolTip(Me.txtTotalsEvent, "Totals Change Event.")
        '
        'lboxInformations
        '
        Me.lboxInformations.FormattingEnabled = True
        Me.lboxInformations.Location = New System.Drawing.Point(1192, -4)
        Me.lboxInformations.Name = "lboxInformations"
        Me.lboxInformations.Size = New System.Drawing.Size(174, 82)
        Me.lboxInformations.TabIndex = 63
        '
        'lblRunInfo
        '
        Me.lblRunInfo.AutoSize = True
        Me.lblRunInfo.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(161, Byte))
        Me.lblRunInfo.ForeColor = System.Drawing.SystemColors.ActiveCaptionText
        Me.lblRunInfo.Location = New System.Drawing.Point(1038, 88)
        Me.lblRunInfo.Name = "lblRunInfo"
        Me.lblRunInfo.Size = New System.Drawing.Size(15, 13)
        Me.lblRunInfo.TabIndex = 65
        Me.lblRunInfo.Text = "A"
        '
        'txtReserved2
        '
        Me.txtReserved2.Location = New System.Drawing.Point(52, 87)
        Me.txtReserved2.Name = "txtReserved2"
        Me.txtReserved2.Size = New System.Drawing.Size(118, 20)
        Me.txtReserved2.TabIndex = 92
        '
        'cmdReserved2
        '
        Me.cmdReserved2.Location = New System.Drawing.Point(5, 86)
        Me.cmdReserved2.Name = "cmdReserved2"
        Me.cmdReserved2.Size = New System.Drawing.Size(41, 21)
        Me.cmdReserved2.TabIndex = 91
        Me.cmdReserved2.Text = "Store"
        Me.cmdReserved2.UseVisualStyleBackColor = True
        '
        'txtReserved1
        '
        Me.txtReserved1.Location = New System.Drawing.Point(52, 60)
        Me.txtReserved1.Name = "txtReserved1"
        Me.txtReserved1.Size = New System.Drawing.Size(118, 20)
        Me.txtReserved1.TabIndex = 90
        '
        'cmdReserved1
        '
        Me.cmdReserved1.Location = New System.Drawing.Point(5, 59)
        Me.cmdReserved1.Name = "cmdReserved1"
        Me.cmdReserved1.Size = New System.Drawing.Size(41, 21)
        Me.cmdReserved1.TabIndex = 89
        Me.cmdReserved1.Text = "Store"
        Me.cmdReserved1.UseVisualStyleBackColor = True
        '
        'txtSerialNumber
        '
        Me.txtSerialNumber.Location = New System.Drawing.Point(52, 34)
        Me.txtSerialNumber.Name = "txtSerialNumber"
        Me.txtSerialNumber.Size = New System.Drawing.Size(118, 20)
        Me.txtSerialNumber.TabIndex = 88
        '
        'cmdSerialNumber
        '
        Me.cmdSerialNumber.Location = New System.Drawing.Point(5, 32)
        Me.cmdSerialNumber.Name = "cmdSerialNumber"
        Me.cmdSerialNumber.Size = New System.Drawing.Size(41, 21)
        Me.cmdSerialNumber.TabIndex = 87
        Me.cmdSerialNumber.Text = "Store"
        Me.cmdSerialNumber.UseVisualStyleBackColor = True
        '
        'txtDescription
        '
        Me.txtDescription.Location = New System.Drawing.Point(52, 7)
        Me.txtDescription.Name = "txtDescription"
        Me.txtDescription.Size = New System.Drawing.Size(118, 20)
        Me.txtDescription.TabIndex = 86
        '
        'cmdDescription
        '
        Me.cmdDescription.Location = New System.Drawing.Point(5, 5)
        Me.cmdDescription.Name = "cmdDescription"
        Me.cmdDescription.Size = New System.Drawing.Size(41, 21)
        Me.cmdDescription.TabIndex = 85
        Me.cmdDescription.Text = "Store"
        Me.cmdDescription.UseVisualStyleBackColor = True
        '
        'btnGetController
        '
        Me.btnGetController.Location = New System.Drawing.Point(176, 7)
        Me.btnGetController.Name = "btnGetController"
        Me.btnGetController.Size = New System.Drawing.Size(60, 49)
        Me.btnGetController.TabIndex = 93
        Me.btnGetController.Text = "Get Controller"
        Me.btnGetController.UseVisualStyleBackColor = True
        '
        'gridUSB_Scan
        '
        Me.gridUSB_Scan.AllowUserToAddRows = False
        Me.gridUSB_Scan.AllowUserToResizeColumns = False
        Me.gridUSB_Scan.AllowUserToResizeRows = False
        Me.gridUSB_Scan.BackgroundColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(192, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.gridUSB_Scan.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.AA, Me.Description, Me.Serial_Number})
        Me.gridUSB_Scan.GridColor = System.Drawing.SystemColors.ControlText
        Me.gridUSB_Scan.Location = New System.Drawing.Point(5, 5)
        Me.gridUSB_Scan.Name = "gridUSB_Scan"
        Me.gridUSB_Scan.ReadOnly = True
        Me.gridUSB_Scan.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.[Single]
        Me.gridUSB_Scan.RowHeadersVisible = False
        Me.gridUSB_Scan.RowHeadersWidth = 35
        Me.gridUSB_Scan.Size = New System.Drawing.Size(312, 120)
        Me.gridUSB_Scan.TabIndex = 100
        '
        'AA
        '
        Me.AA.Frozen = True
        Me.AA.HeaderText = "A/A"
        Me.AA.Name = "AA"
        Me.AA.ReadOnly = True
        Me.AA.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.AA.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.AA.Width = 30
        '
        'Description
        '
        Me.Description.Frozen = True
        Me.Description.HeaderText = "Description"
        Me.Description.Name = "Description"
        Me.Description.ReadOnly = True
        Me.Description.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Description.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Description.Width = 130
        '
        'Serial_Number
        '
        Me.Serial_Number.Frozen = True
        Me.Serial_Number.HeaderText = "Serial Number"
        Me.Serial_Number.Name = "Serial_Number"
        Me.Serial_Number.ReadOnly = True
        Me.Serial_Number.Resizable = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Serial_Number.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.Serial_Number.Width = 130
        '
        'btnBlock
        '
        Me.btnBlock.Location = New System.Drawing.Point(1426, 9)
        Me.btnBlock.Name = "btnBlock"
        Me.btnBlock.Size = New System.Drawing.Size(70, 23)
        Me.btnBlock.TabIndex = 104
        Me.btnBlock.Text = "Block"
        Me.btnBlock.UseVisualStyleBackColor = True
        '
        'btnResume
        '
        Me.btnResume.Location = New System.Drawing.Point(1426, 60)
        Me.btnResume.Name = "btnResume"
        Me.btnResume.Size = New System.Drawing.Size(70, 23)
        Me.btnResume.TabIndex = 106
        Me.btnResume.Text = "Resume"
        Me.btnResume.UseVisualStyleBackColor = True
        '
        'frmAction
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.GradientInactiveCaption
        Me.ClientSize = New System.Drawing.Size(1508, 656)
        Me.Controls.Add(Me.txtTotalsEvent)
        Me.Controls.Add(Me.txtDeliveryEvent)
        Me.Controls.Add(Me.txtPressetsEvent)
        Me.Controls.Add(Me.txtStatusEvent)
        Me.Controls.Add(Me.btnResume)
        Me.Controls.Add(Me.txtBlock)
        Me.Controls.Add(Me.btnBlock)
        Me.Controls.Add(Me.btnGetTotalsUsed)
        Me.Controls.Add(Me.btnAskDeliveriesUsed)
        Me.Controls.Add(Me.btnAskSettingsUsed)
        Me.Controls.Add(Me.gridUSB_Scan)
        Me.Controls.Add(Me.cmbDeviceModel)
        Me.Controls.Add(Me.txtAsciiTransparent)
        Me.Controls.Add(Me.btnGetTotals)
        Me.Controls.Add(Me.btnAskDeliveries)
        Me.Controls.Add(Me.btnAskSettings)
        Me.Controls.Add(Me.cmbTotalNozzles)
        Me.Controls.Add(Me.btnGetController)
        Me.Controls.Add(Me.txtReserved2)
        Me.Controls.Add(Me.cmdReserved2)
        Me.Controls.Add(Me.txtReserved1)
        Me.Controls.Add(Me.cmdReserved1)
        Me.Controls.Add(Me.txtSerialNumber)
        Me.Controls.Add(Me.cmdSerialNumber)
        Me.Controls.Add(Me.txtDescription)
        Me.Controls.Add(Me.cmdDescription)
        Me.Controls.Add(Me.lblRunInfo)
        Me.Controls.Add(Me.lboxInformations)
        Me.Controls.Add(Me.txtHexTransparent)
        Me.Controls.Add(Me.txtPresetAmount)
        Me.Controls.Add(Me.cmdPresetAmount)
        Me.Controls.Add(Me.txtPresetVolume)
        Me.Controls.Add(Me.cmdPresetVolume)
        Me.Controls.Add(Me.txtChangePrice)
        Me.Controls.Add(Me.cmdChangePrice)
        Me.Controls.Add(Me.btnRestart)
        Me.Controls.Add(Me.txtOutTransparent)
        Me.Controls.Add(Me.cmdClearList)
        Me.Controls.Add(Me.cmdAutoRun)
        Me.Controls.Add(Me.cmdStoreSets)
        Me.Controls.Add(Me.cmdSendTrans)
        Me.Controls.Add(Me.lblChannel)
        Me.Controls.Add(Me.gridAction)
        Me.Controls.Add(Me.cmbCommDatas)
        Me.Controls.Add(Me.txtElapseSeconds)
        Me.Controls.Add(Me.cmbCommStyle)
        Me.Controls.Add(Me.cmbCommSpeed)
        Me.Controls.Add(Me.cmbCommPort)
        Me.Controls.Add(Me.txtActionTimes)
        Me.Controls.Add(Me.txtExplane)
        Me.Controls.Add(Me.cmbProducts)
        Me.Controls.Add(Me.lblWorkWithChannel)
        Me.Controls.Add(Me.cmbIdentity)
        Me.Controls.Add(Me.txtIdentity)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "frmAction"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = " Communication  with Controller AP_40"
        CType(Me.gridAction, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridUSB_Scan, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents gridAction As System.Windows.Forms.DataGridView
    Friend WithEvents txtExplane As System.Windows.Forms.TextBox
    Friend WithEvents txtHexTransparent As System.Windows.Forms.TextBox
    Friend WithEvents cmdAutoRun As System.Windows.Forms.Button
    Friend WithEvents tmrAutoRun As System.Windows.Forms.Timer
    Friend WithEvents txtActionTimes As System.Windows.Forms.TextBox
    Friend WithEvents cmbDeviceModel As System.Windows.Forms.ComboBox
    Friend WithEvents cmbIdentity As System.Windows.Forms.ComboBox
    Friend WithEvents cmbCommPort As System.Windows.Forms.ComboBox
    Friend WithEvents cmdStoreSets As System.Windows.Forms.Button
    Friend WithEvents cmbCommSpeed As System.Windows.Forms.ComboBox
    Friend WithEvents cmbCommStyle As System.Windows.Forms.ComboBox
    Friend WithEvents txtElapseSeconds As System.Windows.Forms.TextBox
    Friend WithEvents cmbCommDatas As System.Windows.Forms.ComboBox
    Friend WithEvents txtOutTransparent As System.Windows.Forms.TextBox
    Friend WithEvents lblChannel As System.Windows.Forms.Label
    Friend WithEvents cmdSendTrans As System.Windows.Forms.Button
    Friend WithEvents tmrExtra As System.Windows.Forms.Timer
    Friend WithEvents lblWorkWithChannel As System.Windows.Forms.Label
    Friend WithEvents cmdClearList As System.Windows.Forms.Button
    Friend WithEvents cmbProducts As System.Windows.Forms.ComboBox
    Friend WithEvents btnRestart As System.Windows.Forms.Button
    Friend WithEvents cmdChangePrice As System.Windows.Forms.Button
    Friend WithEvents txtChangePrice As System.Windows.Forms.TextBox
    Friend WithEvents txtPresetVolume As System.Windows.Forms.TextBox
    Friend WithEvents cmdPresetVolume As System.Windows.Forms.Button
    Friend WithEvents txtPresetAmount As System.Windows.Forms.TextBox
    Friend WithEvents cmdPresetAmount As System.Windows.Forms.Button
    Friend WithEvents txtIdentity As System.Windows.Forms.TextBox
    Friend WithEvents ToolTip1 As System.Windows.Forms.ToolTip
    Friend WithEvents lboxInformations As System.Windows.Forms.ListBox
    Friend WithEvents lblRunInfo As System.Windows.Forms.Label
    Friend WithEvents txtReserved2 As System.Windows.Forms.TextBox
    Friend WithEvents cmdReserved2 As System.Windows.Forms.Button
    Friend WithEvents txtReserved1 As System.Windows.Forms.TextBox
    Friend WithEvents cmdReserved1 As System.Windows.Forms.Button
    Friend WithEvents txtSerialNumber As System.Windows.Forms.TextBox
    Friend WithEvents cmdSerialNumber As System.Windows.Forms.Button
    Friend WithEvents txtDescription As System.Windows.Forms.TextBox
    Friend WithEvents cmdDescription As System.Windows.Forms.Button
    Friend WithEvents btnGetController As System.Windows.Forms.Button
    Friend WithEvents cmbTotalNozzles As System.Windows.Forms.ComboBox
    Friend WithEvents btnAskSettings As System.Windows.Forms.Button
    Friend WithEvents btnAskDeliveries As System.Windows.Forms.Button
    Friend WithEvents btnGetTotals As System.Windows.Forms.Button
    Friend WithEvents txtAsciiTransparent As System.Windows.Forms.TextBox
    Friend WithEvents gridUSB_Scan As System.Windows.Forms.DataGridView
    Friend WithEvents AA As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Description As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Serial_Number As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents btnAskSettingsUsed As System.Windows.Forms.Button
    Friend WithEvents btnAskDeliveriesUsed As System.Windows.Forms.Button
    Friend WithEvents btnGetTotalsUsed As System.Windows.Forms.Button
    Friend WithEvents btnBlock As System.Windows.Forms.Button
    Friend WithEvents txtBlock As System.Windows.Forms.TextBox
    Friend WithEvents btnResume As System.Windows.Forms.Button
    Friend WithEvents txtStatusEvent As System.Windows.Forms.TextBox
    Friend WithEvents txtPressetsEvent As System.Windows.Forms.TextBox
    Friend WithEvents txtDeliveryEvent As System.Windows.Forms.TextBox
    Friend WithEvents txtTotalsEvent As System.Windows.Forms.TextBox
    Friend WithEvents Chan As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents NzNz As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Points As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents PStat As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents PErr As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents APStat As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Master As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Alive As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Block As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Auth As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents GetTra As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents NeedA As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents OnFill As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents NzOut As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents DPrice As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents DAmou As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents DVolu As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Price As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents CAmou As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents CVolu As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents TAmou As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents TVolu As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Pomp As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Iden As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Nozz As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Port As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Styl As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Sped As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Data As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Prod As System.Windows.Forms.DataGridViewTextBoxColumn
End Class
