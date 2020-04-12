using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.IO;

using CommonTypes;


namespace XL
{
    public partial class ObjectDisplayForm : Form
    {
        public ObjectDisplayForm(string key)
        {
            InitializeComponent();

            object oo = XLOM.Get(key);
            
            var serializer = new DataContractSerializer(oo.GetType());
            string xmlString;
            using (var sw = new StringWriter())
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    serializer.WriteObject(writer, oo);
                    writer.Flush();
                    xmlString = sw.ToString();
                }
            }


            var xd = new XmlDocument();
            xd.LoadXml(xmlString);

            treeView1.Nodes.Clear();
            treeView1.Nodes.Add(new TreeNode(xd.DocumentElement.Name));

            TreeNode tNode = new TreeNode();
            tNode = treeView1.Nodes[0];

            AddNode(xd.DocumentElement, tNode);
            tNode.Expand();
            //treeView1.ExpandAll();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            Hide();
            Dispose();
        }


        private void AddNode(XmlNode inXmlNode, TreeNode inTreeNode)
        {
            XmlNode xNode;
            TreeNode tNode;
            XmlNodeList nodeList;
            int i;

            // Loop through the XML nodes until the leaf is reached.
            // Add the nodes to the TreeView during the looping process.
            if (inXmlNode.HasChildNodes)
            {
                nodeList = inXmlNode.ChildNodes;
                for (i = 0; i <= nodeList.Count - 1; i++)
                {
                    xNode = inXmlNode.ChildNodes[i];
                    inTreeNode.Nodes.Add(new TreeNode(xNode.Name));
                    tNode = inTreeNode.Nodes[i];
                    AddNode(xNode, tNode);
                }
            }
            else
            {
                // Here you need to pull the data from the XmlNode based on the
                // type of node, whether attribute values are required, and so forth.
                inTreeNode.Text = (inXmlNode.OuterXml).Trim();
            }
        }

        private void ObjectDisplayForm_Load(object sender, EventArgs e)
        {

        }
    }
}
