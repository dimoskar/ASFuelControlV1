using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuelControl.Data;

namespace ASFuelControl.Windows.ViewModels
{
    public partial class InvoiceRelationViewModel
    {
        private List<InvoiceLineRelationViewModel> lineRelations = new List<InvoiceLineRelationViewModel>();

        public InvoiceLineRelationViewModel[] LineRelations
        {
            get { return this.lineRelations.Where(v => v.EntityState != EntityStateEnum.Deleted).ToArray(); }
        }

        public void AddRelation(InvoiceLineRelationViewModel line)
        {
            this.lineRelations.Add(line);
        }

        public override bool Save(DatabaseModel db, Guid id)
        {
            bool saved = base.Save(db, id);
            foreach (InvoiceLineRelationViewModel line in lineRelations)
                line.Save(db, line.InvoiceLineRelationId);
            return saved;
        }
    }
}
