using System;
using System.Collections.Generic;
using System.Text;

namespace PTSLib.PTS
{
    /// <summary>
    /// Provides information about a PTS controller parameter.
    /// </summary>
    public class PtsParameter
    {
        /// <summary>
        /// Gets a value of parameter address.
        /// </summary>
        public short ParameterAddress
        { get; internal set; }

        /// <summary>
        /// Gets a value of parameter number.
        /// </summary>
        public int ParameterNumber
        { get; internal set; }

        /// <summary>
        /// Gets a value of parameter.
        /// </summary>
        public byte[] ParameterValue
        { get; internal set; }
    }
}
