﻿using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace DETI_MakerLab
{
    public partial class CreateKit : Page, DMLPages
    {
        private SqlConnection cn;
        private List<ResourceItem> ResourceItems;
        private ObservableCollection<Kit> KitsListData;
        private ObservableCollection<ListItem> EquipmentsListData;
        private int _staffID;

        internal int StaffID
        {
            get { return _staffID; }
            set { _staffID = value; }
        }

        private bool addResourceItemUnit(ElectronicUnit unit)
        {
            // Add new resource unit
            foreach (ResourceItem item in ResourceItems)
            {
                if (item.Resource.Equals(unit.Model))
                {
                    item.addUnit(unit);
                    return true;
                }
            }
            return false;
        }

        public CreateKit(int StaffID)
        {
            InitializeComponent();
            this.StaffID = StaffID;
            KitsListData = new ObservableCollection<Kit>();
            EquipmentsListData = new ObservableCollection<ListItem>();
            ResourceItems = new List<ResourceItem>();
            try
            {
                // Load kits and resources to it's lists
                LoadKits();
                LoadResources();
            }
            catch (SqlException exc)
            {
                Helpers.ShowCustomDialogBox(exc);
            }
            catch (Exception exc)
            {
                System.Windows.MessageBox.Show(exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            available_kits.ItemsSource = KitsListData;
            units_list.ItemsSource = EquipmentsListData;
        }


        private void LoadKits()
        {
            cn = Helpers.getSGBDConnection();
            if (!Helpers.verifySGBDConnection(cn))
                throw new Exception("Cannot connect to database");

            SqlCommand cmd = new SqlCommand("SELECT * FROM DML.Kit", cn);
            SqlDataReader reader = cmd.ExecuteReader();

            // Dummy kit
            KitsListData.Add(new Kit(-1, "None")); 

            while (reader.Read())
            {
                Kit Resource = new Kit(
                    int.Parse(reader["ResourceID"].ToString()),
                    reader["KitDescription"].ToString()
                    );

                KitsListData.Add(Resource);
            }

            cn.Close();

            // Load kit's units
            LoadUnits();
        }

        private void LoadUnits()
        {
            foreach (Kit kit in KitsListData)
            {
                if (kit.ResourceID == -1)
                    continue;

                DataSet ds = new DataSet();
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = cn;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@KitID", kit.ResourceID);
                cmd.CommandText = "DML.KIT_UNITS";
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);

                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    ElectronicResources resource = new ElectronicResources(
                        row["ProductName"].ToString(),
                        row["Manufacturer"].ToString(),
                        row["Model"].ToString(),
                        row["ResDescription"].ToString(),
                        null,
                        row["PathToImage"].ToString()
                        );

                    ElectronicUnit unit = new ElectronicUnit(
                        int.Parse(row["ResourceID"].ToString()),
                        resource,
                        row["Supplier"].ToString()
                        );

                    kit.addUnit(unit);
                }
            }
        }


        private void LoadResources()
        {
            cn = Helpers.getSGBDConnection();
            if (!Helpers.verifySGBDConnection(cn))
                throw new Exception("Cannot connect to database");

            DataSet ds = new DataSet();
            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Connection = cn;
            cmd.CommandText = "DML.RESOURCES_TO_REQUEST";
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);
            cn.Close();

            foreach (DataRow row in ds.Tables[0].Rows)
            {
                ElectronicResources resource = new ElectronicResources(
                    row["ProductName"].ToString(),
                    row["Manufacturer"].ToString(),
                    row["Model"].ToString(),
                    row["ResDescription"].ToString(),
                    null,
                    row["PathToImage"].ToString()
                    );

                ElectronicUnit unit = new ElectronicUnit(
                    int.Parse(row["ResourceID"].ToString()),
                    resource,
                    row["Supplier"].ToString()
                    );

                if (!addResourceItemUnit(unit))
                {
                    ResourceItem ri = new ResourceItem(resource);
                    ri.addUnit(unit);
                    ResourceItems.Add(ri);
                }
            }

            cn.Close();

            foreach (ResourceItem ri in ResourceItems)
                EquipmentsListData.Add(ri);
        }

        private void SelectKit(Kit kit)
        {
            // Replace item's name by empty string if it hasn't units
            kit_name.Text = kit.ResourceID == -1 ? "" : kit.Description;

            foreach (ListItem resource in units_list.Items)
            {
                ResourceItem ri = resource as ResourceItem;
                int toAdd = 0;
                
                // Get kit's units number
                if (kit.ResourceID != -1)
                    foreach (ElectronicUnit unit in kit.Units)
                        if (ri.Resource.Equals(unit.Model))
                            toAdd++;

                // Find the listbox template fields
                var container = units_list.ItemContainerGenerator.ContainerFromItem(resource) as FrameworkElement;
                ContentPresenter listBoxItemCP = Helpers.FindVisualChild<ContentPresenter>(container);
                if (listBoxItemCP == null)
                    throw new Exception("Invalid item");

                DataTemplate dataTemplate = listBoxItemCP.ContentTemplate;

                DecimalUpDown unitsTextBox = (DecimalUpDown)units_list.ItemTemplate.FindName("equipment_units", listBoxItemCP);
                int units = kit.ResourceID == -1 ? 0 : toAdd;
                unitsTextBox.Value = units;
            }
        }

        private void checkMandatoryFields()
        {
            // Check if all mandatory fields are fill
            if (String.IsNullOrEmpty(kit_name.Text))
                throw new Exception("Please fill in the mandatory fields!");
        }

        public bool isEmpty()
        {
            // Check if any fields are filled in
            if (!String.IsNullOrEmpty(kit_name.Text))
                return false;
            return true;
        }

        private DataTable verifyUnits()
        {
            DataTable toRequest = new DataTable();
            toRequest.Clear();
            toRequest.Columns.Add("ResourceID", typeof(int));

            foreach (ListItem resource in units_list.Items)
            {
                ResourceItem ri = resource as ResourceItem;

                var container = units_list.ItemContainerGenerator.ContainerFromItem(resource) as FrameworkElement;
                ContentPresenter listBoxItemCP = Helpers.FindVisualChild<ContentPresenter>(container);
                if (listBoxItemCP == null)
                    throw new Exception("Invalid item");

                DataTemplate dataTemplate = listBoxItemCP.ContentTemplate;

                int units = int.Parse(((DecimalUpDown)units_list.ItemTemplate.FindName("equipment_units", listBoxItemCP)).Text);
                if (units > ri.Units.Count)
                    throw new Exception("You cannot request more units than the available!");

                // Save each unit on DB
                while (units > 0)
                {
                    ElectronicUnit unit = ri.requestUnit();
                    DataRow row = toRequest.NewRow();
                    row["ResourceID"] = unit.ResourceID;
                    toRequest.Rows.Add(row);
                    units--;
                }
            }

            if (toRequest.Rows.Count == 0)
                throw new Exception("You need to select at least one unit!");

            return toRequest;
        }


        private Kit submitKitCreation(DataTable toRequest)
        {
            Kit newKit = null;
            cn = Helpers.getSGBDConnection();
            if (!Helpers.verifySGBDConnection(cn))
                throw new Exception("Cannot connect to database");

            DataSet ds = new DataSet();
            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Connection = cn;
            cmd.Parameters.Clear();
            SqlParameter listParam = cmd.Parameters.AddWithValue("@UnitsList", toRequest);
            listParam.SqlDbType = SqlDbType.Structured;
            cmd.Parameters.Add("@KitID", SqlDbType.Int).Direction = ParameterDirection.Output;
            cmd.Parameters.AddWithValue("@StaffID", StaffID);
            cmd.Parameters.AddWithValue("@KitDescription", kit_name.Text);
            cmd.CommandText = "DML.CREATE_KIT";
            int kitID = -1;

            try
            {
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
                kitID = Convert.ToInt32(cmd.Parameters["@KitID"].Value);
                newKit = new Kit(kitID, kit_name.Text);

                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    ElectronicResources resource = new ElectronicResources(
                        row["ProductName"].ToString(),
                        row["Manufacturer"].ToString(),
                        row["Model"].ToString(),
                        row["ResDescription"].ToString(),
                        null,
                        row["PathToImage"].ToString()
                        );

                    ElectronicUnit unit = new ElectronicUnit(
                        int.Parse(row["ResourceID"].ToString()),
                        resource,
                        row["Supplier"].ToString()
                        );

                    newKit.addUnit(unit);
                }
            }
            catch (SqlException ex)
            {
                throw ex;
            }
            finally
            {
                cn.Close();
            }

            return newKit;
        }

        private void equipment_info_Click(object sender, RoutedEventArgs e)
        {
            // Go to selected equipment's page
            ResourceItem equipment = (ResourceItem)(sender as Button).DataContext;
            StaffWindow window = (StaffWindow) Window.GetWindow(this);
            window.goToEquipmentPage(equipment.Resource);
        }

        private void create_kit_button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                checkMandatoryFields();
                DataTable toRequest = verifyUnits();

                MessageBoxResult confirm = System.Windows.MessageBox.Show(
                    "Do you confirm the kit creation?",
                    "Kit Creation Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                    );
                if (confirm == MessageBoxResult.Yes)
                {
                    Kit kit = submitKitCreation(toRequest);
                    System.Windows.MessageBox.Show("Kit has been succesfully created!");
                    StaffWindow window = (StaffWindow)Window.GetWindow(this);
                    window.goToKitPage(kit, true);
                }
            }
            catch (SqlException exc)
            {
                Helpers.ShowCustomDialogBox(exc);
            }
            catch (Exception exc)
            {
                System.Windows.MessageBox.Show(exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextBox_TextChanged_Equipments(object sender, TextChangedEventArgs e)
        {
            // Filter equipments which contains writed keyword
            if (EquipmentsListData.Count > 0 && !search_box_equipments.Text.Equals(""))
            {
                var filteredEquipments = EquipmentsListData.Where(i => ((ListItem)i).ToString().ToLower().Contains(search_box_equipments.Text.ToLower())).ToArray();
                units_list.ItemsSource = filteredEquipments;
            }
            else
            {
                units_list.ItemsSource = EquipmentsListData;
            }
        }

        private void available_kits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Fill the form's with the selected kit template
            Kit k = available_kits.SelectedItem as Kit;
            SelectKit(k);
        }
    }
}
