using System;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;

namespace QBFormApplication
{
    public partial class quickbooksimportinvoiceform : Form
    {
        public quickbooksimportinvoiceform()
        {
            InitializeComponent();
        }

        private void quickbooksimportinvoiceform_Load(object sender, EventArgs e)
        {
            var consumerKey = "qyprd6B6Au9m7884naCWDN8tvY2Caq";
            var consumerSecret = "RAL8lWJg7XVgqmK7wxUvpDkxKU6wPlyXeS2f9C5O";
            var accessToken = "qyprd0vfAPr3b1SVO1E1DfhnsgwtR5FhBz8Emvwt3TBAIiYP";
            var accessTokenSecret = "WwVHcN3v3OkMajLRtKiezD7zrnxoOp031ni7OIL4";
            var realmId = "b7q2mdjahd";
            //IntuitServicesType serviceType = (IntuitServicesType)HttpContext.Current.Session["intuitServiceType"];

            var validator = new OAuthRequestValidator(accessToken, accessTokenSecret, consumerKey, consumerSecret);
            var context = new ServiceContext("c7162f08b4380b4381b9178baf698830e6cd", realmId, IntuitServicesType.QBO, validator as Intuit.Ipp.Security.IRequestValidator);
            var service = new DataService(context);



            //Find Customer
            QueryService<Customer> customerQueryService = new QueryService<Customer>(context);
            Customer customer = new Customer();
            try
            {
               customer = customerQueryService.ExecuteIdsQuery("Select * From Customer StartPosition 4 MaxResults 1").FirstOrDefault<Customer>();
            }
            catch (Exception ex)
            {
                using (TextWriter writer = File.CreateText("C:\\temp\\loggingexception.txt"))
                {
                    writer.WriteLine(ex);
                }
            }
            //Find Tax Code for Invoice - Searching for a tax code named 'StateSalesTax' in this example
            QueryService<TaxCode> stateTaxCodeQueryService = new QueryService<TaxCode>(context);
            TaxCode stateTaxCode = stateTaxCodeQueryService.ExecuteIdsQuery("Select * From TaxCode Where Name='StateSalesTax' StartPosition 1 MaxResults 1").FirstOrDefault();

            //Find Account - Accounts Receivable account required
            QueryService<Account> accountQueryService = new QueryService<Account>(context);
            Account account = accountQueryService.ExecuteIdsQuery("Select * From Account Where AccountType='Accounts Receivable' StartPosition 1 MaxResults 1").FirstOrDefault();

            //Find Item
            QueryService<Item> itemQueryService = new QueryService<Item>(context);
            Item item = itemQueryService.ExecuteIdsQuery("Select * From Item StartPosition 1 MaxResults 1").FirstOrDefault();

            //Find Term
            QueryService<Term> termQueryService = new QueryService<Term>(context);
            Term term = termQueryService.ExecuteIdsQuery("Select * From Term StartPosition 1 MaxResults 1").FirstOrDefault();


            Invoice invoice = new Invoice();

            //DocNumber - QBO Only, otherwise use DocNumber
            invoice.AutoDocNumber = true;
            invoice.AutoDocNumberSpecified = true;

            //TxnDate
            invoice.TxnDate = DateTime.Now.Date;
            invoice.TxnDateSpecified = true;

            //PrivateNote
            invoice.PrivateNote = "This is a private note";

            //Line
            Line invoiceLine = new Line();
            //Line Description
            invoiceLine.Description = "Invoice line description.";
            //Line Amount
            invoiceLine.Amount = 330m;
            invoiceLine.AmountSpecified = true;
            //Line Detail Type
            invoiceLine.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
            invoiceLine.DetailTypeSpecified = true;
            //Line Sales Item Line Detail
            SalesItemLineDetail lineSalesItemLineDetail = new SalesItemLineDetail();
            //Line Sales Item Line Detail - ItemRef
            lineSalesItemLineDetail.ItemRef = new ReferenceType()
            {
                name = item.Name,
                Value = item.Id
            };
            //Line Sales Item Line Detail - UnitPrice
            lineSalesItemLineDetail.AnyIntuitObject = 33m;
            lineSalesItemLineDetail.ItemElementName = ItemChoiceType.UnitPrice;
            //Line Sales Item Line Detail - Qty
            lineSalesItemLineDetail.Qty = 10;
            lineSalesItemLineDetail.QtySpecified = true;
            //Line Sales Item Line Detail - TaxCodeRef
            //For US companies, this can be 'TAX' or 'NON'
            lineSalesItemLineDetail.TaxCodeRef = new ReferenceType()
            {
                Value = "TAX"
            };
            //Line Sales Item Line Detail - ServiceDate
            lineSalesItemLineDetail.ServiceDate = DateTime.Now.Date;
            lineSalesItemLineDetail.ServiceDateSpecified = true;
            //Assign Sales Item Line Detail to Line Item
            invoiceLine.AnyIntuitObject = lineSalesItemLineDetail;
            //Assign Line Item to Invoice
            invoice.Line = new Line[] { invoiceLine };

            //TxnTaxDetail
            TxnTaxDetail txnTaxDetail = new TxnTaxDetail();
            txnTaxDetail.TxnTaxCodeRef = new ReferenceType()
            {
                name = stateTaxCode.Name,
                Value = stateTaxCode.Id
            };
            Line taxLine = new Line();
            taxLine.DetailType = LineDetailTypeEnum.TaxLineDetail;
            TaxLineDetail taxLineDetail = new TaxLineDetail();
            //Assigning the fist Tax Rate in this Tax Code
            taxLineDetail.TaxRateRef = stateTaxCode.SalesTaxRateList.TaxRateDetail[0].TaxRateRef;
            taxLine.AnyIntuitObject = taxLineDetail;
            txnTaxDetail.TaxLine = new Line[] { taxLine };
            invoice.TxnTaxDetail = txnTaxDetail;

            //Customer (Client)
            invoice.CustomerRef = new ReferenceType()
            {
                name = customer.DisplayName,
                Value = customer.Id
            };

            //Billing Address
            PhysicalAddress billAddr = new PhysicalAddress();
            billAddr.Line1 = "123 Main St.";
            billAddr.Line2 = "Unit 506";
            billAddr.City = "Brockton";
            billAddr.CountrySubDivisionCode = "MA";
            billAddr.Country = "United States";
            billAddr.PostalCode = "02301";
            billAddr.Note = "Billing Address Note";
            invoice.BillAddr = billAddr;

            //Shipping Address
            PhysicalAddress shipAddr = new PhysicalAddress();
            shipAddr.Line1 = "100 Fifth Ave.";
            shipAddr.City = "Waltham";
            shipAddr.CountrySubDivisionCode = "MA";
            shipAddr.Country = "United States";
            shipAddr.PostalCode = "02452";
            shipAddr.Note = "Shipping Address Note";
            invoice.ShipAddr = shipAddr;

            //SalesTermRef
            invoice.SalesTermRef = new ReferenceType()
            {
                name = term.Name,
                Value = term.Id
            };

            //DueDate
            invoice.DueDate = DateTime.Now.AddDays(30).Date;
            invoice.DueDateSpecified = true;

            //ARAccountRef
            invoice.ARAccountRef = new ReferenceType()
            {
                name = account.Name,
                Value = account.Id
            };

            Invoice invoiceAdded = service.Add(invoice);


        }
    }
}
