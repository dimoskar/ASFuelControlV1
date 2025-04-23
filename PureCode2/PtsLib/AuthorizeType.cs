using System;
using System.Collections.Generic;
using System.Text;

namespace PTSLib.PTS
{
    /// <summary>
    /// Specifies a type of authorization for commands Authorize for a FuelPoint.
    /// </summary>
    public enum AuthorizeType
    {
        /// <summary>
        /// Authorization by volume.
        /// </summary>
        Volume = 0x4C,
        /// <summary>
        /// Authorization by money amount.
        /// </summary>
        Amount = 0x50
    };
}
