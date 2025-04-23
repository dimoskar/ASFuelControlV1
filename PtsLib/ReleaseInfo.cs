using System;
using System.Collections.Generic;
using System.Text;

namespace PTSLib.PTS
{
    /// <summary>
    /// Provides information about a firmware version of PTS controller.
    /// </summary>
    public class ReleaseInfo
    {
        /// <summary>
        /// Gets a firmware version date of PTS controller.
        /// </summary>
        public DateTime ReleaseDate
        { get; internal set; }

        /// <summary>
        /// Gets a firmware version number of PTS controller.
        /// </summary>
        public short ReleaseNum
        { get; internal set; }

        /// <summary>
        /// Gets a full release firmware version name of PTS controller.
        /// </summary>
        public string ReleaseVersion
        { get; internal set; }

        /// <summary>
        /// Gets a list of supported FuelPoint communication protocols by a firmware version of PTS controller.
        /// </summary>
        public string[] SupportedFuelPointProtocols
        { get; internal set; }

        /// <summary>
        /// Gets a list of supported ATG communication protocols by a firmware version of PTS controller.
        /// </summary>
        public string[] SupportedAtgProtocols
        { get; internal set; }
    }
}
