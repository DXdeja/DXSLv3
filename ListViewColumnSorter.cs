using System.Collections;
using System.Windows.Forms;
using System;

namespace dxsl
{
    /// <summary>
    /// This class is an implementation of the 'IComparer' interface.
    /// </summary>
    public class ListViewColumnSorter : IComparer
    {
        /// <summary>
        /// Specifies the column to be sorted
        /// </summary>
        protected int ColumnToSort;
        /// <summary>
        /// Specifies the order in which to sort (i.e. 'Ascending').
        /// </summary>
        protected SortOrder OrderOfSort;
        /// <summary>
        /// Case insensitive comparer object
        /// </summary>
        private CaseInsensitiveComparer ObjectCompare;

        /// <summary>
        /// Class constructor.  Initializes various elements
        /// </summary>
        public ListViewColumnSorter()
        {
            // Initialize the column to '0'
            ColumnToSort = 0;

            // Initialize the sort order to 'none'
            OrderOfSort = SortOrder.None;

            // Initialize the CaseInsensitiveComparer object
            ObjectCompare = new CaseInsensitiveComparer();
        }

        /// <summary>
        /// This method is inherited from the IComparer interface.  It compares the two objects passed using a case insensitive comparison.
        /// </summary>
        /// <param name="x">First object to be compared</param>
        /// <param name="y">Second object to be compared</param>
        /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
        public virtual int Compare(object x, object y)
        {
            int compareResult;
            ListViewItem listviewX, listviewY;

            // Cast the objects to be compared to ListViewItem objects
            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;

            // Compare the two items
            try
            {
                string a = listviewX.SubItems[ColumnToSort].Text;
                string b = listviewY.SubItems[ColumnToSort].Text;
                compareResult = ObjectCompare.Compare(a, b);
            }
            catch
            {
                return 0;
            }

            // Calculate correct return value based on object comparison
            if (OrderOfSort == SortOrder.Ascending)
            {
                // Ascending sort is selected, return normal result of compare operation
                return compareResult;
            }
            else if (OrderOfSort == SortOrder.Descending)
            {
                // Descending sort is selected, return negative result of compare operation
                return (-compareResult);
            }
            else
            {
                // Return '0' to indicate they are equal
                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
        /// </summary>
        public int SortColumn
        {
            set
            {
                ColumnToSort = value;
            }
            get
            {
                return ColumnToSort;
            }
        }

        /// <summary>
        /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
        /// </summary>
        public SortOrder Order
        {
            set
            {
                OrderOfSort = value;
            }
            get
            {
                return OrderOfSort;
            }
        }
    }

    public class DXSLMainSorter : ListViewColumnSorter
    {
        public override int Compare(object x, object y)
        {
            int compareResult = 0;
            
            switch (ColumnToSort)
            {
                case 6:
                    // ping
                    int a, b;
                    try
                    {
                        a = int.Parse(((ListViewItem)x).SubItems[ColumnToSort].Text);
                        b = int.Parse(((ListViewItem)y).SubItems[ColumnToSort].Text);
                    }
                    catch
                    {
                        return 0;
                    }

                    compareResult = a - b;

                    break;

                case 5:
                    // players
                    // push servers with players on in front
                    Form1.server_ s1, s2;
                    try
                    {
                        s1 = (Form1.server_)((ListViewItem)x).Tag;
                        s2 = (Form1.server_)((ListViewItem)y).Tag;
                        compareResult = int.Parse(s2.props.numplayers) - int.Parse(s1.props.numplayers);

                        if (compareResult == 0) // now push servers with max players
                        {
                            compareResult = int.Parse(s2.props.maxplayers) - int.Parse(s1.props.maxplayers);
                        }
                    }
                    catch
                    {
                        return 0;
                    }

                    break;

                case 1:
                    // IP sort
                    
                    try
                    {
                        s1 = (Form1.server_)((ListViewItem)x).Tag;
                        s2 = (Form1.server_)((ListViewItem)y).Tag;
                        byte[] ipb1 = s1.address.Address.GetAddressBytes();
                        byte[] ipb2 = s2.address.Address.GetAddressBytes();
                        Array.Reverse(ipb1);
                        Array.Reverse(ipb2);
                        uint ip1 = BitConverter.ToUInt32(ipb1, 0);
                        uint ip2 = BitConverter.ToUInt32(ipb2, 0);

                        if (ip1 > ip2) compareResult = 1;
                        else if (ip1 < ip2) compareResult = -1;
                        else compareResult = 0;

                        if (compareResult == 0)
                        {
                            // compare ports
                            compareResult = int.Parse(s1.props.hostport) - int.Parse(s2.props.hostport);
                        }
                    }
                    catch
                    {
                        return 0;
                    }

                    break;

                default:
                    return base.Compare(x, y);
            }

            if (OrderOfSort == SortOrder.Ascending)
                return compareResult;
            else if (OrderOfSort == SortOrder.Descending)
                return (-compareResult);
            else
                return 0;
        }
    }
}
