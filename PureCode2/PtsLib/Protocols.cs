using System;
using System.Collections.Generic;
using System.Text;

namespace PTSLib.PTS
{
    /// <summary>
    /// Specifies a list of supported communication protocols of FuelPoint channels of a PTS controller.
    /// </summary>
    public enum FuelPointChannelProtocol
    {
        /// <summary>
        /// Communication protocol is not set.
        /// </summary>
        None = 0,
        /// <summary>
        /// Protocol ADAST EASYCALL.
        /// </summary>
        ADAST_EASYCALL = 1,
        /// <summary>
        /// Protocol TIT UNIPUMP.
        /// </summary>
        TIT_UNIPUMP = 2,
        /// <summary>
        /// Protocol WAYNE DART.
        /// </summary>
        WAYNE_DART = 3,
        /// <summary>
        /// Protocol MMPETRO ZAP.
        /// </summary>
        MMPETRO_ZAP = 4,
        /// <summary>
        /// Protocol GILBARCO TWO-WIRE.
        /// </summary>
        GILBARCO_TWO_WIRE = 5,
        /// <summary>
        /// Protocol TOKHEIM (Tokheim Controller-Dispenser Communication protocol).
        /// </summary>
        TOKHEIM = 6,
        /// <summary>
        /// Protocol Tatsuno BENC PDE.
        /// </summary>
        TATSUNO_BENC_PDE = 7,
        /// <summary>
        /// Protocol DEVELCO.
        /// </summary>
        DEVELCO = 8,
        /// <summary>
        /// Protocol SAFE Graf.
        /// </summary>
        SAFE_GRAF = 9,
        /// <summary>
        /// Protocol Pump Control GALILEO.
        /// </summary>
        GALILEO_PUMP_CONTROL = 10,
        /// <summary>
        /// Protocol TOKHEIM (5 nozzles).
        /// </summary>
        TOKHEIM_5_nozzles = 11,
        /// <summary>
        /// Protocol SLAVUTICH FD-Link.
        /// </summary>
        SLAVUTICH_FD_LINK = 12,
        /// <summary>
        /// Protocol PUMP_SIMULATOR_10.
        /// </summary>
        PUMP_SIMULATOR_10 = 13,
        /// <summary>
        /// Protocol T10_AR.
        /// </summary>
        SB_T10_AR = 14,
        /// <summary>
        /// Protocol TATSUNO SS-LAN.
        /// </summary>
        TATSUNO_SSLAN = 15,
        /// <summary>
        /// Protocol SHELF.
        /// </summary>
        SHELF = 16,
        /// <summary>
        /// Protocol UNIGAZ.
        /// </summary>
        UNIGAZ = 17,
        /// <summary>
        /// Protocol BATCHEN_Electroline.
        /// </summary>
        BATCHEN_Electroline = 18,
        /// <summary>
        /// Protocol PUMALAN.
        /// </summary>
        PUMALAN = 19,
        /// <summary>
        /// Protocol KOREA_EnE.
        /// </summary>
        KOREA_EnE = 20,
        /// <summary>
        /// Protocol BENNETT_CL.
        /// </summary>
        BENNETT_CL = 21,
        /// <summary>
        /// Protocol BENNETT_RS485.
        /// </summary>
        BENNETT_RS485 = 22,
        /// <summary>
        /// Protocol WAYNE_USCL.
        /// </summary>
        WAYNE_USCL = 23,
        /// <summary>
        /// Protocol NUOVO_PIGNONE_CL.
        /// </summary>
        NUOVO_PIGNONE_CL = 24,
        /// <summary>
        /// Protocol PEC.
        /// </summary>
        PEC = 25,
        /// <summary>
        /// Protocol BLUE_SKY.
        /// </summary>
        BLUE_SKY = 26,
        /// <summary>
        /// Protocol PROWALCO.
        /// </summary>
        PROWALCO = 27,
        /// <summary>
        /// Protocol TOKICO_SS_LAN.
        /// </summary>
        TOKICO_SS_LAN = 28,
        /// <summary>
        /// Protocol SANKI.
        /// </summary>
        SANKI = 29,
        /// <summary>
        /// Protocol LANFENG.
        /// </summary>
        LANFENG = 30,
        /// <summary>
        /// Protocol DONG HWA Prime.
        /// </summary>
        DONG_HWA_Prime = 31,
        /// <summary>
        /// Protocol EPCO.
        /// </summary>
        EPCO = 32,
        /// <summary>
        /// Protocol WAYNE DART SIMPLEX.
        /// </summary>
        WAYNE_DART_SIMPLEX = 33,
        /// <summary>
        /// Protocol KALVACHA.
        /// </summary>
        KALVACHA = 34,
        /// <summary>
        /// Protocol HONG_YANG_FZ.
        /// </summary>
        HONG_YANG_FZ = 35,
        /// <summary>
        /// Protocol KRAUS MNET.
        /// </summary>
        KRAUS_MNET = 36,
        /// <summary>
        /// Protocol PUMP SIMULATOR.
        /// </summary>
        PUMP_SIMULATOR = 37,
        /// <summary>
        /// Protocol HONG_YANG_886.
        /// </summary>
        HONG_YANG_886 = 38,
        /// <summary>
        /// Protocol BATCHEN_MPP.
        /// </summary>
        BATCHEN_MPP = 39,
        /// <summary>
        /// Protocol TOMINAGA_SS_LAN.
        /// </summary>
        TOMINAGA_SS_LAN = 40,
        /// <summary>
        /// Protocol TOPAZ.
        /// </summary>
        TOPAZ = 41,
        /// <summary>
        /// Protocol HONG_YANG_MPD_886.
        /// </summary>
        HONG_YANG_MPD_886 = 42,
        /// <summary>
        /// Protocol FALCON_LPG.
        /// </summary>
        FALCON_LPG = 43,
        /// <summary>
        /// Protocol TOPAZ_WONS.
        /// </summary>
        TOPAZ_WONS = 44
    }

    /// <summary>
    /// Specifies a list of supported communication protocols of ATG channels of a PTS controller.
    /// </summary>
    public enum AtgChannelProtocol
    {
        /// <summary>
        /// Communication protocol is not set.
        /// </summary>
        None = 0,
        /// <summary>
        /// Protocol GILBARCO Veeder Root TLS 3.5.
        /// </summary>
        VEEDER_ROOT = 1,
        /// <summary>
        /// Protocol Start Italiana.
        /// </summary>
        START_ITALIANA = 2,
        /// <summary>
        /// Protocol PetrolVend 4.
        /// </summary>
        PETROVEND = 3,
        /// <summary>
        /// Protocol Struna 1.4.
        /// </summary>
        STRUNA_1_4 = 4,
        /// <summary>
        /// Protocol FAFNIR_VISY_Quick.
        /// </summary>
        FAFNIR_VISY_Quick = 5,
        /// <summary>
        /// Protocol ASSYTECH.
        /// </summary>
        ASSYTECH = 6,
        /// <summary>
        /// Protocol ATG_SIMULATOR.
        /// </summary>
        ATG_SIMULATOR = 7,
        /// <summary>
        /// Protocol HECTRONIC_HLS.
        /// </summary>
        HECTRONIC_HLS = 8,
        /// <summary>
        /// Protocol UNIPROBE.
        /// </summary>
        UNIPROBE = 9
    }
}
