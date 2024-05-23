using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DuckCreek.BranchManagerApp.BankService;
using System.Xml.Linq;
using System.Xml;
using System.ServiceModel;

namespace DuckCreek.BranchManagerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            List<BranchDetail> springfieldBranches = new List<BranchDetail>();
            List<BranchDetail> invalidBranches = new List<BranchDetail>();
            List<BranchDetail> recentlyChangedBranches = new List<BranchDetail>();

            List<BranchDetail> branchDetails = GetBranches("MO");

            foreach(BranchDetail branch in branchDetails)
            {
                if((branch.City).Equals("Springfield", StringComparison.InvariantCultureIgnoreCase))
                {
                    springfieldBranches.Add(branch);

                    if (!ValidateRoutingNumber(branch.RoutingNumber))
                    {
                        invalidBranches.Add(branch);
                    }
                }

                if (CheckChangeDate(branch.ChangeDate))
                {
                    recentlyChangedBranches.Add(branch);
                }
            }
            CreateXml(springfieldBranches, "SpringfieldBranches.xml");
            CreateXml(invalidBranches, "InvalidBranches.xml");
            CreateXml(recentlyChangedBranches, "RecentlyChanged.xml");
        }

        public static List<BranchDetail> GetBranches(string stateCode)
        {
            List<BranchDetail> branches = new List<BranchDetail>();
            BankServiceClient client = new BankServiceClient();
            try
            {
                BranchDetail[] branchDetails = client.GetBanksForState(stateCode);
                foreach(BranchDetail branch in branchDetails)
                {
                    if((branch.StateCode).Equals(stateCode, StringComparison.InvariantCultureIgnoreCase))
                    {
                        branches.Add(branch);
                    }
                }
            }
            catch (Exception ex)
            {
                
                if (client.InnerChannel.State == System.ServiceModel.CommunicationState.Faulted)
                {
                    client.Abort();
                    throw new FaultException(ex.Message);
                }
                else
                {
                    client.Abort();
                    throw new Exception(ex.Message);
                }
            }
            finally
            {
                client.Close();
            }
            return branches;

        }

        public static bool CheckChangeDate(string changeDate)
        {
            bool recent = false;
            DateTime dateResult;
            if(DateTime.TryParse(changeDate.Substring(0, 10), out dateResult))
            {
                if(dateResult > DateTime.Parse("01/01/2017"))
                {
                    recent = true;
                }
            }
            else
            {
                Console.WriteLine("Invalid Change Date format: " + changeDate);
            }
            return recent;
        }

        public static bool ValidateRoutingNumber(string routingNumber)
        {
            bool valid = false;
            if(routingNumber.Length == 9)
            {
                int productSum = 0;
                for(int i = 0; i < 9; ++i)
                {
                    switch((i + 1) % 3)
                    {
                        case 1:
                            productSum += ((int)Char.GetNumericValue(routingNumber[i]) * 3);
                            break;
                        case 2:
                            productSum += ((int)Char.GetNumericValue(routingNumber[i]) * 7);
                            break;
                        case 0:
                            productSum += (int)Char.GetNumericValue(routingNumber[i]);
                            break;
                    }
                }
                if(productSum % 10 == 0)
                {
                    valid = true;
                }
            }
            return valid;
        }

        public static void CreateXml(List<BranchDetail> branches, string filename)
        {
            using (XmlWriter writer = XmlWriter.Create(filename))
            {
                writer.WriteStartElement("branches");
                foreach (BranchDetail branch in branches)
                {
                    writer.WriteStartElement("branch");
                    writer.WriteElementString("BranchName", branch.BranchName);
                    writer.WriteElementString("RoutingNumber", branch.RoutingNumber);
                    //if (!string.IsNullOrWhiteSpace(branch.NewRoutingNumber.Trim('0')))
                    //{
                    //    writer.WriteElementString("NewRoutingNumber", branch.NewRoutingNumber);
                    //}
                    writer.WriteElementString("NewRoutingNumber", branch.NewRoutingNumber);
                    writer.WriteElementString("Address", branch.Address);
                    writer.WriteElementString("City", branch.City);
                    writer.WriteElementString("StateCode", branch.StateCode);
                    writer.WriteElementString("ZipCode", branch.ZipCode);
                    //if (!string.IsNullOrWhiteSpace(branch.ZipExtension.Trim('0')))
                    //{
                    //    writer.WriteElementString("ZipExtension", branch.ZipExtension);
                    //}
                    writer.WriteElementString("ZipExtension", branch.ZipExtension);
                    writer.WriteElementString("ChangeDate", branch.ChangeDate);
                    writer.WriteEndElement();
                }
                writer.Flush();
            }
        }
    }
}
