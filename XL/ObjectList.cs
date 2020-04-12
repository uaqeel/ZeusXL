using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace XL
{
    public partial class ObjectList : Form
    {
        public ObjectList()
        {
            InitializeComponent();
            
            foreach (OMKey ok in XLOM.GetKeys())
            {
                xLOMBindingSource.Add(ok);
            }
        }


        private void DisplayObject(object sender, EventArgs e)
        {
            DataGridViewRow row = (dataGridView1.SelectedRows.Count > 0 ? dataGridView1.SelectedRows[0] : dataGridView1.Rows[dataGridView1.SelectedCells[0].RowIndex]);
            OMKey key = new OMKey((string)row.Cells[0].Value, (string)row.Cells[1].Value, (int)row.Cells[2].Value);

            ObjectDisplayForm odf = new ObjectDisplayForm(key.ToString());
            odf.Show();
        }


        private void DeleteObject(object sender, DataGridViewRowCancelEventArgs e)
        {
            OMKey key = new OMKey((string)e.Row.Cells[0].Value, (string)e.Row.Cells[1].Value, (int)e.Row.Cells[2].Value);
            XLOM.Remove(key.ToString());
        }
    }
}
