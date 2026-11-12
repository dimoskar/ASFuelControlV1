using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common.Enumerators
{
    public enum InvoiceRelationTypeEnum
    {
        Cancel = 0,
        Replace = 1,
        // Retains local transformation provenance and prevents the copied
        // fiscal values from being recalculated. It is never transmitted as
        // a myDATA/provider invoice correlation.
        LocalCopy = 2,
        // Makes the child unavailable to the automatic print/issue worker
        // until the parent document has completed printing. This relation is
        // local workflow metadata and is never transmitted as a fiscal
        // correlation.
        ProcessingDependency = 3
    }
}
