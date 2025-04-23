Imports System.IO

Module CommWork_AK
    Private bread As Integer
    Private bwritten As Integer
    Private AK_ftdi As Integer
    Private AK_txbuffer(128) As Byte
    Private AK_txlen As Integer
    Private AK_rxbuffer(1024) As Byte
    Private AK_rxpointer As Integer = 0
    Private AK_Hw485 As Boolean
    Private AK_Sides(32) As Integer
    Private AK_ReplyData As String
    Private AK_ID As Integer
    Private AK_RealID As Integer
    Friend AK_ID_Offset As Integer


    Private Sub Set_AK_id(ByVal idn As Integer, ByVal idrl As Integer)
        AK_ID = idn
        AK_RealID = idrl
    End Sub



    Private Function AK_TxSend(ByVal extra_command As Integer) As Boolean
        Dim txtry As Integer = 5
        While txtry <> 0
            Try
                If AK_ID >= 1 Then
                    If FT_Purge(AK_ftdi, FT_PURGE_RX Or FT_PURGE_TX) = FT_OK Then
                        Dim cntr_tx(4) As Byte
                        cntr_tx(0) = &HAA
                        cntr_tx(1) = &H55
                        cntr_tx(2) = AK_txlen           ' Total Bytes After Extra Command.
                        cntr_tx(3) = AK_ID
                        cntr_tx(4) = extra_command
                        If FT_Write_Bytes(AK_ftdi, cntr_tx(0), 5, bwritten) = FT_OK Then
                            If FT_Write_Bytes(AK_ftdi, AK_txbuffer(0), AK_txlen, bwritten) = FT_OK Then Return True
                        End If
                    End If
                Else
                    If FT_Purge(AK_ftdi, FT_PURGE_RX Or FT_PURGE_TX) = FT_OK Then
                        If FT_Write_Bytes(AK_ftdi, AK_txbuffer(0), AK_txlen, bwritten) = FT_OK Then Sleep(50) : Return True
                    End If
                End If

            Catch ex As Exception
            End Try
            txtry -= 1
            Sleep(50)
        End While
        Return False
    End Function





    Private Function AK_ReadRX(ByVal sty As Integer) As Integer
        Try
            Dim rxlen As Integer

            Dim rxchar As Byte
            Dim PacketType As Integer
            Dim PacketLength As Integer
            Dim rxtimer_upd As Integer
            If AK_Hw485 Then
                rxtimer_upd = 20
            Else
                rxtimer_upd = 50
            End If



AK_RX_Loop:

            Dim rxtimer As Integer
            rxtimer = rxtimer_upd

            Dim StartByte1 As Boolean = False
            Dim StartByte2 As Boolean = False
            Dim startbyte3 As Boolean = False

            If Not AK_Hw485 Then FT_Purge(AK_ftdi, FT_PURGE_TX And FT_PURGE_RX)
            If Not AK_Hw485 Then Sleep(50)

            AK_rxpointer = 0

            Select Case AK_txbuffer(1)
                Case &HF
                    PacketType = 2
                Case Else
                    PacketType = 1

            End Select


            While rxtimer > 0
                If FT_GetQueueStatus(AK_ftdi, rxlen) <> FT_OK Then Return 2

                Select Case rxlen
                    Case 0
                        Sleep(10)
                        rxtimer -= 1
                        Continue While
                    Case Is >= 256
                        If FT_Read_Bytes(AK_ftdi, AK_rxbuffer(0), rxlen, bread) <> FT_OK Then Return 2
                        Return 4                     'BUFFER OVERFLOW

                    Case Else
                        If PacketType = 1 Then
                            If Not StartByte2 Then rxtimer = rxtimer_upd Else rxtimer = 10
                        End If

                        If PacketType = 2 Then
                            rxtimer = 10
                        End If
                End Select

                'AK_RxStack -= 1 : If AK_RxStack = 0 Then Return 5



                'BYTES RECEIVED
                If FT_Read_Bytes(AK_ftdi, rxchar, 1, bread) <> FT_OK Then Return 2

                AK_rxbuffer(AK_rxpointer) = rxchar
                AK_rxpointer += 1

                If Not StartByte1 Then
                    If rxchar = &H10 AndAlso PacketType = 1 Then StartByte1 = True
                    If rxchar = &H22 AndAlso PacketType = 2 Then StartByte1 = True
                    If StartByte1 Then AK_rxbuffer(0) = rxchar : AK_rxpointer = 1 : Continue While
                    If PacketType = 1 Then rxtimer = rxtimer_upd
                    AK_rxpointer = 0
                    Continue While
                End If

                If Not StartByte2 Then
                    If PacketType = 1 AndAlso rxchar = &H9 Then
                        StartByte2 = True
                        Select Case AK_Sides(AK_RealID)
                            Case 1
                                PacketLength = &H10 - 2
                            Case 2
                                PacketLength = &H20 - 2
                            Case 3
                                PacketLength = &H30 - 2
                            Case 4
                                PacketLength = &H40 - 2
                            Case 5
                                PacketLength = &H50 - 2
                            Case 6
                                PacketLength = &H60 - 2

                        End Select
                    End If
                    If PacketType = 2 AndAlso rxchar = &HA Then StartByte2 = True : PacketLength = &H22 - 2 : StartByte2 = True
                    If StartByte2 Then AK_rxbuffer(1) = rxchar : AK_rxpointer = 2 : Continue While
                    StartByte1 = False
                    AK_rxpointer = 0
                    If PacketType = 1 Then rxtimer = rxtimer_upd
                    Continue While
                End If

                If Not startbyte3 Then
                    If rxchar = sty Then
                        startbyte3 = True : PacketLength -= 1 : startbyte3 = True : AK_rxbuffer(2) = rxchar : AK_rxpointer = 3
                        If PacketType = 1 Then
                            rxtimer = 5
                        Else
                            rxtimer = 10
                        End If
                        Continue While
                    Else
                        StartByte1 = False
                        StartByte2 = False
                        AK_rxpointer = 0
                        If PacketType = 1 Then rxtimer = rxtimer_upd
                        Continue While
                    End If
                End If




                'CHECK PACKET FINISHED
                If PacketType = 2 Then
                    PacketLength -= 1
                    If PacketLength = 0 Then
                        Dim akcrc As Integer = 0
                        For i = 0 To AK_rxpointer - 2
                            akcrc = (akcrc + AK_rxbuffer(i)) And 255
                        Next
                        If akcrc = AK_rxbuffer(AK_rxpointer - 1) Then Return 1
                        Return 3
                    End If
                End If

                If PacketType = 1 Then
                    PacketLength -= 1
                    If PacketLength = 0 Then
                        If AK_rxpointer = 3 Then PacketLength = 0
                        Return 1
                    End If

                End If
            End While




            'TIMEOUT
            If AK_rxpointer = 0 Then
                Return 0
            Else
                If PacketType = 1 Then GoTo ak_rx_loop
                Return 3
            End If


        Catch ex As Exception
            Return 0
        End Try
    End Function

    '0 TIMEOUT
    '1 ANSWER OK
    '2 FTDI ERROR
    '3 DATA ERROR
    '4 DATA OVERFLOW

    Private Sub AK_MakeCRC()
        Dim ak_crc As Integer = 0
        For i = 0 To AK_txlen
            ak_crc = (ak_crc + AK_txbuffer(i)) And 255
        Next
        AK_txbuffer(AK_txlen + 1) = ak_crc
        AK_txlen += 2
    End Sub

    'ΕΡΩΤΗΣΗ ΓΙΑ ΝΑ ΜΗΝ ΚΑΝΕΙ TIMEOUT Η ΑΝΤΛΙΑ, ΕΠΙΣΤΡΕΦΕΙ STATUS
    '1-ΣΦΑΛΜΑ
    '2-ΘΕΛΕΙ INITIALIZE
    '3-ΠΙΣΤΟΛΙ ΚΑΤΩ
    '4-ΠΙΣΤΟΛΙ ΠΑΝΩ, ANAMONH AUTHORIZE
    '5-ΜΗΔΕΝΙΚΗ ΠΑΡΑΔΟΣΗ, ΤΟ ΠΙΣΤΟΛΙ ΑΝ ΣΗΚΩΘΕΙ ΔΕ ΘΑ ΖΗΤΗΣΕΙ AUTHORIZE
    '6,7,8,9 - ΣΕ ΠΑΡΑΔΟΣΗ
    Private Function AK_Status(ByVal stb3 As Integer) As Integer
        AK_txbuffer(0) = 2
        AK_txbuffer(1) = 7
        AK_txbuffer(2) = stb3
        AK_txlen = 2
        AK_MakeCRC()
        If Not AK_TxSend(0) Then Return 2 'FTDI ERROR
        Return AK_ReadRX(stb3)
    End Function

    Private Function AK_TxStatus(ByVal stb3 As Integer) As Integer
        AK_txbuffer(0) = 2
        AK_txbuffer(1) = 7
        AK_txbuffer(2) = stb3
        AK_txlen = 2
        AK_MakeCRC()
        If Not AK_TxSend(0) Then Return 2 'FTDI ERROR
        Return 1
    End Function

    Private Function AK_Block(ByVal stb3 As Integer) As Integer
        AK_txbuffer(0) = 2
        AK_txbuffer(1) = 2
        AK_txbuffer(2) = stb3
        AK_txlen = 2
        AK_MakeCRC()
        If Not AK_TxSend(0) Then Return 2 'FTDI ERROR
        Return 1
    End Function

    'OTAN TO STATUS EINAI 2, ΤΟ ΣΗΜΕΙΟ ΠΩΛΗΣΗΣ ΘΕΛΕΙ INITIALIZE
    Private Function AK_Init(ByVal stb3 As Integer) As Integer
        AK_txbuffer(0) = 2
        AK_txbuffer(1) = 1
        AK_txbuffer(2) = stb3
        AK_txlen = 2
        AK_MakeCRC()
        If Not AK_TxSend(0) Then Return 2 'FTDI ERROR
        Return 1
    End Function

    'OTAN TO STATUS EINAI 2, ΤΟ ΣΗΜΕΙΟ ΠΩΛΗΣΗΣ ΘΕΛΕΙ INITIALIZE
    Private Function AK_Init2(ByVal stb3 As Integer) As Integer
        AK_txbuffer(0) = 2
        AK_txbuffer(1) = &HC
        AK_txbuffer(2) = stb3
        AK_txlen = 2
        AK_MakeCRC()
        If Not AK_TxSend(0) Then Return 2 'FTDI ERROR
        Return 1
    End Function

    'ΟΤΑΝ ΤΟ ΠΙΣΤΟΛΙ ΚΑΝΕΙ ΜΗΔΕΝΙΚΗ ΠΑΡΑΔΟΣΗ ΔΙΝΕΙ STATUS 5. ΘΕΛΕΙ RESET ΓΙΑ ΝΑ ΞΑΝΑΠΑΕΙ ΣΕ STATUS 3
    Private Function AK_Reset(ByVal stb3 As Integer) As Integer
        AK_txbuffer(0) = 2
        AK_txbuffer(1) = 2
        AK_txbuffer(2) = stb3
        AK_txlen = 2
        AK_MakeCRC()
        If Not AK_TxSend(0) Then Return 2 'FTDI ERROR
        Return 1
        'Return AK_ReadRX(stb3)
    End Function


    'ΕΡΩΤΗΣΗ ΓΙΑ TOTALS ΣΗΜΕΙΟΥ ΠΩΛΗΣΗΣ

    Private Function AK_GetTotals(ByVal stb3 As Integer) As Integer
        AK_txbuffer(0) = 2

        AK_txbuffer(1) = &HF
        AK_txbuffer(2) = stb3
        AK_txlen = 2
        AK_MakeCRC()
        If Not AK_TxSend(&HA1) Then Return 2 'FTDI ERROR
        Return AK_ReadRX(stb3)
    End Function


    'ΣΕΤΑΡΙΣΜΑ ΤΙΜΗΣ ΣΤΟ ΣΗΜΕΙΟ ΠΩΛΗΣΗΣ
    Private Function AK_SetPrice(ByVal stb3 As Integer, ByVal NewPrice1 As String, ByVal NewPrice2 As String, NewPrice3 As String, NewPrice4 As String, NewPrice5 As String) As Integer
        AK_txbuffer(0) = &HC    'LENGTH
        AK_txbuffer(1) = &H8    'PACKET TYPE
        AK_txbuffer(2) = stb3   'DELIVERY STATION
        AK_txbuffer(3) = Val("&H" & NewPrice1.Substring(0, 2))  'PRICE NOZZLE 1 HIGH
        AK_txbuffer(4) = Val("&H" & NewPrice1.Substring(2, 2))  'PRICE NOZZLE 1 LOW
        AK_txbuffer(5) = Val("&H" & NewPrice2.Substring(0, 2))
        AK_txbuffer(6) = Val("&H" & NewPrice2.Substring(2, 2))
        AK_txbuffer(7) = Val("&H" & NewPrice3.Substring(0, 2))
        AK_txbuffer(8) = Val("&H" & NewPrice3.Substring(2, 2))
        AK_txbuffer(9) = Val("&H" & NewPrice4.Substring(0, 2))
        AK_txbuffer(10) = Val("&H" & NewPrice4.Substring(2, 2))
        AK_txbuffer(11) = Val("&H" & NewPrice5.Substring(0, 2))
        AK_txbuffer(12) = Val("&H" & NewPrice5.Substring(2, 2))
        AK_txlen = 12
        AK_MakeCRC()
        If Not AK_TxSend(0) Then Return 2 'FTDI ERROR
        Return 1
    End Function





    'AUTHORIZE ΧΩΡΙΣ PRESET ΣΤΟ ΣΗΜΕΙΟ ΠΩΛΗΣΗΣ
    Private Function AK_Auth(ByVal stb3 As Integer) As Integer
        AK_txbuffer(0) = &H8    'LENGTH
        AK_txbuffer(1) = 3  'PACKET TYPE
        AK_txbuffer(2) = stb3 'DELIVERY STATION
        AK_txbuffer(3) = 0                          ' Volume
        AK_txbuffer(4) = 0
        AK_txbuffer(5) = 0
        AK_txbuffer(6) = 0                          ' Amount
        AK_txbuffer(7) = 0
        AK_txbuffer(8) = 0
        AK_txlen = 8
        AK_MakeCRC()
        If Not AK_TxSend(0) Then Return 2
        Return 1
    End Function

    'AUTHORIZE ΜΕ PRESET ΣΤΟ ΣΗΜΕΙΟ ΠΩΛΗΣΗΣ
    Private Function AK_AuthPreset(ByVal stb3 As Integer, ByVal PresetValue As Double) As Integer
        AK_txbuffer(0) = &H8
        AK_txbuffer(1) = 3
        AK_txbuffer(2) = stb3
        AK_txbuffer(3) = 0
        AK_txbuffer(4) = 0
        AK_txbuffer(5) = 0
        Dim tmpre As String = PresetValue.ToString(Format("0000.00"))
        AK_txbuffer(6) = Val("&H" & tmpre.Substring(0, 2))
        AK_txbuffer(7) = Val("&H" & tmpre.Substring(2, 2))
        AK_txbuffer(8) = Val("&H" & tmpre.Substring(5, 2))
        AK_txlen = 8
        AK_MakeCRC()
        If Not AK_TxSend(0) Then Return 2
        Return 1
    End Function





    Dim AK_SideNozzleUp(32, 6) As Integer
    Dim Ak_Authorized(32, 8) As Boolean


    Function AK_CheckCRC(ByVal StartB As Integer, ByVal StopB As Integer) As Boolean
        Dim akcrc As Integer = 0
        For i = StartB To StopB - 1
            akcrc = (akcrc + AK_rxbuffer(i)) And 255
        Next
        If akcrc = AK_rxbuffer(StopB) Then Return True Else Return False
    End Function


    Function AK_FindSide(ByVal idd As Integer, nzz As Integer) As Integer
        Dim cs As Integer = id_status(idd, nzz, st_nozzlenum)
        Return Int(AllNozzles(cs).SpecialFunction / 10)
    End Function




    Sub AK_SideSetStatus(ByVal channel As Integer, ByVal Sidenum As Integer, ByVal St As Integer)
        For i = 1 To 32
            If AllNozzles(i).PumpType <> "11" Then Continue For
            If Not AllNozzles(i).Enable Then Continue For
            If Int(AllNozzles(i).SpecialFunction / 10) = Sidenum AndAlso AllNozzles(i).ID = channel Then
                Dim nzz As Integer = AllNozzles(i).NozzleNumber
                Dim prevst As Integer = id_status(channel, nzz, st_status)
                id_status(channel, nzz, st_status) = St
                If St = 0 AndAlso prevst = 1 Then
                    id_status(channel, nzz, st_status) = 3
                End If
            End If
        Next
    End Sub



    Sub CommExecAK()
        Application.CurrentCulture = New CultureInfo("el-gr")
        AK_Hw485 = AK485

        Dim tmpa As Integer
        Dim txtry As Integer

        Dim AK_Scan(8) As Integer


        'ΟΡΙΣΜΟΣ ΠΛΕΥΡΩΝ ΑΝΑ ID ΟΡΙΖΕΤΑΙ ΑΠΟ ΤΟ ALLNOZZLES SPECIALFUNCTION, ΤΗΝ ΥΨΗΛΟΤΕΡΗ ΔΕΚΑΔΑ

        For i = 1 To 32
            If Not AllNozzles(i).PumpType = "11" Then Continue For
            Dim idt, nzt As Integer
            idt = AllNozzles(i).ID
            nzt = AllNozzles(i).NozzleNumber
            tmpa = Int(AllNozzles(i).SpecialFunction / 10)
            If tmpa > AK_Sides(idt) Then AK_Sides(idt) = tmpa
        Next

        Select Case comm_type


            'DETECT ID
            Case 1
                AK_ftdi = FTDI_dev(11)


                Id_Nz_Scan(11, 0) = 0
                Id_Nz_Scan(11, 1) = 1



                txtry = 2

comm_AK1_l:
                If id_status(Id_Nz_Scan(11, 1), 1, st_pumptype) <> 11 Then GoTo comm_AK1_enda
                Set_AK_id(Id_Nz_Scan(11, 1) - AK_ID_Offset, Id_Nz_Scan(11, 1))


                Sleep(10) : tmpa = AK_Status(1)
                If tmpa = 2 Then GoTo comm_ftdi_error
                If tmpa = 1 Then GoTo comm_AK1_ok


comm_AK1_err:
                txtry -= 1 : If txtry > 0 Then GoTo comm_ak1_l
                GoTo comm_AK1_enda

comm_AK1_ok:
                Id_Nz_Scan(11, 0) += 1
                id_status(Id_Nz_Scan(11, 1), 0, st_active) = 11




comm_AK1_end:
                Sleep(10)
comm_AK1_enda:
                txtry = 2
                If ComSkip = True Then Exit Sub
                Id_Nz_Scan(11, 1) += 1

                If Id_Nz_Scan(11, 1) <= 32 Then GoTo comm_AK1_l


                'SET PRICE
            Case 2














                'GET STATUS
            Case 3
                AK_ftdi = FTDI_dev(11)
                Id_Nz_Scan(11, 1) = 1
                Dim side As Integer = 1

Comm_AK3_l0:
                'CHECK IF CHANNEL IS ACTIVE
                If id_status(Id_Nz_Scan(11, 1), 0, st_active) <> 11 Then GoTo comm_AK3_endb


                Set_AK_id(Id_Nz_Scan(11, 1) - AK_ID_Offset, Id_Nz_Scan(11, 1))

                If Not AK485 Then
                    Sleep(10) : tmpa = AK_TxStatus(1) : Sleep(10)
                    Sleep(50)
                End If
                tmpa = AK_Status(1)
                If tmpa = 2 Then GoTo comm_ftdi_error
                If tmpa = 1 Then GoTo comm_AK3_l
                GoTo comm_ak3_err


                'NOZZLE 1 STATUS


comm_AK3_err:
                For i = 0 To 8
                    id_status(Id_Nz_Scan(11, 1), i, st_comerror) += 1
                Next i

                GoTo comm_ak3_endb



comm_AK3_l:
                If id_status(Id_Nz_Scan(11, 1), 0, st_active) <> 11 Then GoTo comm_AK3_enda
                Select Case AK_Sides(Id_Nz_Scan(11, 1))
                    Case 1
                        If AK_rxpointer < 16 Then GoTo comm_AK3_err
                        If Not AK_CheckCRC(0, 15) Then GoTo comm_ak3_err
                    Case 2
                        If AK_rxpointer < 32 Then GoTo comm_AK3_err
                        If Not AK_CheckCRC(16, 31) Then GoTo comm_ak3_err
                    Case 3
                        If AK_rxpointer < 48 Then GoTo comm_AK3_err
                        If Not AK_CheckCRC(32, 47) Then GoTo comm_ak3_err
                    Case 4
                        If AK_rxpointer < 64 Then GoTo comm_AK3_err
                        If Not AK_CheckCRC(48, 63) Then GoTo comm_ak3_err
                    Case 5
                        If AK_rxpointer < 80 Then GoTo comm_AK3_err
                        If Not AK_CheckCRC(64, 79) Then GoTo comm_ak3_err
                    Case 6
                        If AK_rxpointer < 96 Then GoTo comm_AK3_err
                        If Not AK_CheckCRC(80, 95) Then GoTo comm_ak3_err
                End Select


                'Using writer As StreamWriter = New StreamWriter("C:/temp/log.txt", True, System.Text.Encoding.GetEncoding("Windows-1253"))
                '    Dim tmplog As String = "ID: " & Id_Nz_Scan(11, 1) & ", SIDE: " & side & ", DATA: "
                '    Select Case side
                '        Case 1
                '            For i = 0 To 15
                '                Dim tmpchar As String = AK_rxbuffer(i).ToString("X")
                '                If tmpchar.Length = 1 Then tmpchar = "0" & tmpchar
                '                tmplog &= tmpchar & " "
                '            Next
                '        Case 2
                '            For i = 16 To 31
                '                Dim tmpchar As String = AK_rxbuffer(i).ToString("X")
                '                If tmpchar.Length = 1 Then tmpchar = "0" & tmpchar
                '                tmplog &= tmpchar & " "
                '            Next
                '        Case 3
                '            For i = 32 To 47
                '                Dim tmpchar As String = AK_rxbuffer(i).ToString("X")
                '                If tmpchar.Length = 1 Then tmpchar = "0" & tmpchar
                '                tmplog &= tmpchar & " "
                '            Next

                '        Case 4
                '            For i = 48 To 63
                '                Dim tmpchar As String = AK_rxbuffer(i).ToString("X")
                '                If tmpchar.Length = 1 Then tmpchar = "0" & tmpchar
                '                tmplog &= tmpchar & " "
                '            Next


                '    End Select
                '    writer.Write(tmplog & vbCrLf)
                'End Using




                Dim ak_nzoffset As Integer = 16 * (side - 1)

                If AK_rxbuffer(0 + ak_nzoffset) = &H10 AndAlso AK_rxbuffer(1 + ak_nzoffset) = &H9 AndAlso AK_rxbuffer(2 + ak_nzoffset) = side Then

                Else
                    Sleep(50)
                    GoTo comm_ak3_endb
                End If

                For i = 0 To 8
                    id_status(Id_Nz_Scan(11, 1), i, st_comerror) = 0
                Next



                Dim AK_CurrentStatus As Integer = AK_rxbuffer(3 + ak_nzoffset)
                Dim curnoz As Integer = AK_rxbuffer(4 + ak_nzoffset)
                Dim selnoz As Integer = (side * 10) + curnoz
                Dim allnz As Integer = 0
                Dim nzt, idt As Integer

                'IF THERE IS NOZZLE, FIND THE ALLNOZZLE POSITION, IDT & NZT
                If curnoz <> 0 Then
                    For i = 1 To 32
                        If AllNozzles(i).PumpType = "11" AndAlso AllNozzles(i).ID = Id_Nz_Scan(11, 1) Then
                            If AllNozzles(i).SpecialFunction = selnoz Then
                                allnz = i
                                idt = AllNozzles(i).ID
                                nzt = AllNozzles(i).NozzleNumber
                            End If
                        End If
                    Next
                End If

                
                'CHECK STATUS
                Select Case AK_CurrentStatus


                    Case 0
                        Sleep(10)
                    Case 1
                        'ERROR PROBABLY
                        Sleep(10)

                        'TRANSACTION WITHOUT FILLING
                    Case 5
                        AK_SideSetStatus(Id_Nz_Scan(11, 1), side, 0)
                        Sleep(10)
                        AK_Reset(side)

                        'INITIALIZE REQUIRED
                    Case 2
                        Sleep(10)
                        AK_Init(side)
                        GoTo AK_EndTransaction



                        'NOZZLE IDLE
                    Case 3, 9
                        'FIND IF THERE WAS A NOZZLE UP BEFORE

AK_EndTransaction:
                        Dim prvnz As Integer = AK_SideNozzleUp(Id_Nz_Scan(11, 1), side)
                        AK_SideNozzleUp(Id_Nz_Scan(11, 1), side) = 0
                        If prvnz <> 0 Then
                            Dim NzNum As Integer = (side * 10) + prvnz
                            nzt = 0
                            'FIND NOZZLE
                            For i = 1 To 32
                                If AllNozzles(i).PumpType = "11" AndAlso AllNozzles(i).ID = Id_Nz_Scan(11, 1) Then
                                    If AllNozzles(i).SpecialFunction = NzNum Then
                                        allnz = i
                                        idt = AllNozzles(i).ID
                                        nzt = AllNozzles(i).NozzleNumber
                                        Exit For
                                    End If
                                End If
                            Next
                            '****** ΕΛΕΓΧΟΣ ΥΠΑΡΞΗΣ *********
                            'If nzt = 0 Then AK_SideSetStatus(Id_Nz_Scan(11, 1), side, 0) : Exit Select
                            If nzt = 0 Then Exit Select


                            Dim ak_amount As String = ""
                            For i = 10 + ak_nzoffset To 12 + ak_nzoffset
                                If AK_rxbuffer(i) < &H10 Then
                                    ak_amount &= "0" & AK_rxbuffer(i).ToString("X")
                                Else
                                    ak_amount &= AK_rxbuffer(i).ToString("X")
                                End If
                            Next
                            id_status(idt, nzt, st_amount) = Val(ak_amount) / 100

                            Dim ak_volume As String = ""
                            For i = 7 + ak_nzoffset To 9 + ak_nzoffset
                                If AK_rxbuffer(i) < &H10 Then
                                    ak_volume &= "0" & AK_rxbuffer(i).ToString("X")
                                Else
                                    ak_volume &= AK_rxbuffer(i).ToString("X")
                                End If
                            Next
                            id_status(idt, nzt, st_volume) = Val(ak_volume) / 100
                            If id_status(idt, nzt, st_status) = 1 Then id_status(idt, nzt, st_status) = 3
                        Else
                            AK_SideSetStatus(Id_Nz_Scan(11, 1), side, 0)
                        End If





                    Case 4
                        'AUTHORIZE
                        If id_status(idt, nzt, st_status) <> 2 Then
                            Ak_Authorized(idt, nzt) = False
                            'SET PRICE

                            NewPrice = id_status(idt, nzt, st_price).ToString("0.000")
                            NewPrice = NewPrice.Replace(",", "").Replace(".", "")
                            Select Case NewPrice.Length
                                Case 1
                                    NewPrice = "000" & NewPrice
                                Case 2
                                    NewPrice = "00" & NewPrice
                                Case 3
                                    NewPrice = "0" & NewPrice
                                Case Else
                            End Select
                            Sleep(80)
                            tmpa = AK_SetPrice(side, NewPrice, NewPrice, NewPrice, NewPrice, NewPrice)
                        End If
                        id_status(idt, nzt, st_status) = 2
                        AK_SideNozzleUp(idt, side) = curnoz


                        'FILLING
                    Case 6, 7, 8
                        '6 AUTHORIZED, NO FILLING YET
                        '8 FILLING

                        id_status(idt, nzt, st_status) = 1
                        AK_SideNozzleUp(idt, side) = curnoz
                        Dim ak_amount As String = ""
                        For i = 10 + ak_nzoffset To 12 + ak_nzoffset
                            If AK_rxbuffer(i) < &H10 Then
                                ak_amount &= "0" & AK_rxbuffer(i).ToString("X")
                            Else
                                ak_amount &= AK_rxbuffer(i).ToString("X")
                            End If
                        Next
                        id_status(idt, nzt, st_amount) = Val(ak_amount) / 100

                        Dim ak_volume As String = ""
                        For i = 7 + ak_nzoffset To 9 + ak_nzoffset
                            If AK_rxbuffer(i) < &H10 Then
                                ak_volume &= "0" & AK_rxbuffer(i).ToString("X")
                            Else
                                ak_volume &= AK_rxbuffer(i).ToString("X")
                            End If
                        Next
                        id_status(idt, nzt, st_volume) = Val(ak_volume) / 100


                        If CheckLowLimit(idt, nzt) Then
                            Sleep(50)
                            tmpa = AK_Block(side)
                        End If

                    Case Else
                        tmpa = tmpa



                End Select



comm_AK3_end:
                'Sleep(5)

comm_AK3_enda:
                side += 1
                If side <= AK_Sides(Id_Nz_Scan(11, 1)) Then GoTo comm_AK3_l
                If AK485 Then Sleep(60)
comm_AK3_endb:
                side = 1
                Id_Nz_Scan(11, 1) += 1
                If Id_Nz_Scan(11, 1) <= 32 Then GoTo comm_AK3_l0





                'AUTHORIZE

            Case 4
                Id_Nz_Scan(11, 1) = 1
                Id_Nz_Scan(11, 2) = 1
                txtry = 3
                Dim tmpc As Double

comm_AK4_l:
                If id_status(Id_Nz_Scan(11, 1), 0, st_active) <> 11 Then GoTo comm_ak4_endb
                If id_status(Id_Nz_Scan(11, 1), Id_Nz_Scan(11, 2), st_active) <> 11 Then GoTo comm_AK4_enda
                Set_AK_id(Id_Nz_Scan(11, 1) - AK_ID_Offset, Id_Nz_Scan(11, 1))

                If id_status(Id_Nz_Scan(11, 1), Id_Nz_Scan(11, 2), st_allowauthreq) = 0 Then GoTo comm_ak4_enda
                Dim side As Integer = AK_FindSide(Id_Nz_Scan(11, 1), Id_Nz_Scan(11, 2))

                If Not AK_Hw485 Then

                    'SET PRICE
                    Sleep(50)
                    NewPrice = id_status(Id_Nz_Scan(11, 1), Id_Nz_Scan(11, 2), st_price).ToString("0.000")
                    NewPrice = NewPrice.Replace(",", "").Replace(".", "")
                    Select Case NewPrice.Length
                        Case 1
                            NewPrice = "000" & NewPrice
                        Case 2
                            NewPrice = "00" & NewPrice
                        Case 3
                            NewPrice = "0" & NewPrice
                        Case Else
                    End Select
                    tmpa = AK_SetPrice(side, NewPrice, NewPrice, NewPrice, NewPrice, NewPrice)
                    Sleep(50)
                End If

                tmpc = id_status(Id_Nz_Scan(11, 1), Id_Nz_Scan(11, 2), st_preset)

                Sleep(50)
                If tmpc = 0 Then
                    tmpa = AK_Auth(side)

                Else
                    tmpa = AK_AuthPreset(side, tmpc)
                End If

                Ak_Authorized(Id_Nz_Scan(11, 1), Id_Nz_Scan(11, 2)) = True

                'FIND SIDE

comm_AK4_end:

comm_AK4_enda:

                Id_Nz_Scan(11, 2) += 1
                If Id_Nz_Scan(11, 2) <= 8 Then GoTo comm_AK4_l

                'If AK485 Then Sleep(150)

comm_AK4_endb:
                Id_Nz_Scan(11, 2) = 1

                If id_status(Id_Nz_Scan(11, 1), 0, st_active) = 11 Then
                    Sleep(50) : tmpa = AK_TxStatus(1) : Sleep(50)
                End If

                Id_Nz_Scan(11, 1) += 1
                If Id_Nz_Scan(11, 1) <= 32 Then GoTo comm_AK4_l
                '**********************************************************************
                'SET PRICE

            Case 5

                Id_Nz_Scan(11, 1) = 1
                Id_Nz_Scan(11, 2) = 1

                txtry = 3

comm_AK5_l:
                If id_status(Id_Nz_Scan(11, 1), Id_Nz_Scan(11, 2), st_active) <> 11 Then GoTo comm_AK5_end
                id_status(Id_Nz_Scan(11, 1), Id_Nz_Scan(11, 2), st_priceupdate) = 0
comm_AK5_end:   Id_Nz_Scan(11, 2) += 1
                If Id_Nz_Scan(11, 2) <= 8 Then GoTo comm_AK5_l
                Id_Nz_Scan(11, 2) = 1
                Id_Nz_Scan(11, 1) += 1
                If Id_Nz_Scan(11, 1) <= 32 Then GoTo comm_AK5_l

























                ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                'GET TOTALS
            Case 6
                Dim AK_Totalizer(5) As String

                Id_Nz_Scan(11, 1) = 1
                Dim side As Integer = 1
                txtry = TX_RETRY

comm_AK6_l:
                If id_status(Id_Nz_Scan(11, 1), 0, st_active) <> 11 Then GoTo comm_AK6_enda
                Set_AK_id(Id_Nz_Scan(11, 1) - AK_ID_Offset, Id_Nz_Scan(11, 1))



                'CHECK IF ANY NOZZLE OF THE SIDE'S TOTALIZER IS REQUESTED
                Dim nztotal As Boolean = False
                For i = 1 To 32
                    If AllNozzles(i).PumpType = "11" AndAlso AllNozzles(i).ID = Id_Nz_Scan(11, 1) AndAlso Int(AllNozzles(i).SpecialFunction / 10) = side Then
                        Dim idt, nzt As Integer
                        idt = AllNozzles(i).ID
                        nzt = AllNozzles(i).NozzleNumber
                        If id_status(idt, nzt, st_totalsafterfilling) = 1 Then nztotal = True : Exit For
                    End If
                Next

                If Not nztotal Then GoTo comm_ak6_end



comm_AK6_l1:
                Sleep(50)
                tmpa = AK_GetTotals(side)
                If tmpa = 2 Then GoTo comm_ftdi_error
                If tmpa = 1 AndAlso AK_rxpointer = &H22 Then GoTo comm_AK6_ok

comm_AK6_err:
                txtry -= 1
                If txtry > 0 Then GoTo comm_AK6_l1 'retry
                GoTo comm_ak6_enda

comm_AK6_ok:
                For k = 0 To 4
                    Dim totalstr As String = ""
                    For i = (3 + (k * 6)) To (8 + (k * 6))


                        If AK_rxbuffer(i) < &H10 Then
                            totalstr &= "0" & AK_rxbuffer(i).ToString("X")
                        Else
                            totalstr &= AK_rxbuffer(i).ToString("X")
                        End If
                    Next
                    AK_Totalizer(k + 1) = totalstr
                Next k


                'PUT TOTALIZERS TO CORRECT POSITIONS
                For i = 1 To 32
                    If AllNozzles(i).PumpType = "11" AndAlso Int(AllNozzles(i).SpecialFunction / 10) = side AndAlso AllNozzles(i).ID = Id_Nz_Scan(11, 1) Then
                        Dim nzn As Integer = Val(AllNozzles(i).SpecialFunction.ToString.Substring(1, 1))
                        AllNozzles(i).TotalVolue = AK_Totalizer(nzn) / 100
                        AllNozzles(i).Totalizer.LastDate = Now
                        AllNozzles(i).Totalizer.Updated = True
                    End If
                Next

                'Sleep(10)

comm_AK6_end:
                txtry = TX_RETRY
                side += 1
                If side <= AK_Sides(Id_Nz_Scan(11, 1)) Then GoTo comm_AK6_l

                Sleep(20)
                tmpa = AK_TxStatus(1)

comm_AK6_enda:
                side = 1
                Id_Nz_Scan(11, 1) += 1
                If Id_Nz_Scan(11, 1) <= 32 Then GoTo comm_AK6_l















                'DETECT ID
            Case 16
                AK_ftdi = FTDI_dev(11)
                Id_Nz_Scan(11, 1) = 1
                txtry = 1

comm_AK16_l:
                If id_status(Id_Nz_Scan(11, 1), 1, st_pumptype) <> 11 Then GoTo comm_AK16_enda
                If id_status(Id_Nz_Scan(11, 1), 1, st_active) = 11 Then GoTo comm_AK16_enda

                Set_AK_id(Id_Nz_Scan(11, 1) - AK_ID_Offset, Id_Nz_Scan(11, 1))
                Sleep(10) : tmpa = AK_Status(1)

                If tmpa = 2 Then GoTo comm_ftdi_error
                If tmpa = 1 Then GoTo comm_AK16_ok


comm_AK16_err:
                txtry -= 1 : If txtry > 0 Then GoTo comm_ak16_l
                GoTo comm_AK16_enda
comm_AK16_ok:
                Id_Nz_Scan(11, 0) += 1
                id_status(Id_Nz_Scan(11, 1), 0, st_active) = 11
                id_status(Id_Nz_Scan(11, 1), 1, st_active) = 11


comm_AK16_end:
                Sleep(10)
comm_AK16_enda:
                txtry = 1
                If ComSkip = True Then Exit Sub
                Id_Nz_Scan(11, 1) += 1
                If Id_Nz_Scan(11, 1) <= 32 Then GoTo comm_AK16_l












        End Select
        If AK485 Then
            Select Case Id_Nz_Scan(11, 0)
                Case 0 To 2
                    Sleep(100)
                Case 3 To 6
                    Sleep(50)
                Case 7 To 9
                    Sleep(10)
                Case 10 To 32
                    Sleep(5)
            End Select
        Else
            Sleep(10)
        End If

        Id_Nz_Scan(11, 1) = 32
        Exit Sub
comm_ftdi_error:


        RetryFtdi(11)
    End Sub




End Module
