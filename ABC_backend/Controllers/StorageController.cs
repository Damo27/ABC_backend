using ABC_backend.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace ABC_backend.Controllers
{
    public class StorageController : Controller
    {
        //___________Action to display the Create Department view______________________________
        public ActionResult CreateDepartment()
        {
            return View();
        }
        //___________[HttpPost]Action to accept the department info from the Create Department view and process______________________________
        [HttpPost]
        public ActionResult CreateDepartment(String DepartmentName, String Description)
        {
            try
            {
                CloudBlobContainer blobContainer = StorageManager.GetCloudBlobContainer(DepartmentName);
                if (blobContainer.CreateIfNotExists())
                {
                    BlobContainerPermissions permissions = blobContainer.GetPermissions();
                    permissions.PublicAccess = BlobContainerPublicAccessType.Blob;
                    blobContainer.SetPermissions(permissions);
                    Department department = new Department(DepartmentName) { DepartmentName = DepartmentName, Description = Description, NumberOfItems = 0 };
                    StorageManager.AddDepartment(department);
                    return RedirectToAction("Feedback", "Home", new { message = "Department succesfully added!", isSuccess = true, redirectControl = "Storage", redirectAction = "AddItem" });
                }
                return RedirectToAction("Feedback", "Home", new { message = "Failed to add department. " + DepartmentName + " already exists!", isSuccess = false, redirectControl = "Storage", redirectAction = "AddItem" });
            }
            catch (Exception e)
            {
                return RedirectToAction("Feedback", "Home", new { message = "Failed to add department. " + e.Message, isSuccess = false, redirectControl = "Storage", redirectAction = "AddItem" });
            }

        }
        //___________Action to display the AddItem view______________________________
        public ActionResult AddItem()
        {
            var departments = StorageManager.GetDepartments();
            ViewBag.Departments = new SelectList(departments, "Rowkey", "DepartmentName");
            StorageManager.DepartmentInUse = departments.First().DepartmentName;
            CloudBlobContainer container = StorageManager.GetCloudBlobContainer(StorageManager.DepartmentInUse);
            var blobs = container.ListBlobs();
            List<Item> blobUrls = new List<Item>();
            ParentModel pm = new ParentModel();
            foreach (var blob in blobs)
            {
                blobUrls.Add(new Item { ImgURL = blob.StorageUri.PrimaryUri.ToString() });
            }
            pm.Items = blobUrls;
            return View(pm);
        }
        //___________[HttpPost]Action to accept the item info from the AddItem view and process______________________________
        [HttpPost]
        public ActionResult AddItem(String Departments, String ItemName, String Description, String Price, String ImgUrl)
        {
            double price;
            if (!double.TryParse(Price, out price))
            {
                Price = Price.Replace('.', ',');
            }
            try
            {
                price = double.Parse(Price);
                Item item = new Item(Departments, ItemName) { ItemName = ItemName, Description = Description, Price = price, ImgURL = ImgUrl };
                StorageManager.AddItem(item);
                return RedirectToAction("Feedback", "Home", new { message = item.ItemName + " Successfully Added", isSuccess = true, redirectControl = "Storage", redirectAction = "AddItem" });
            }
            catch (Exception e)
            {
                return RedirectToAction("Feedback", "Home", new { message = "Failed to add Item. " + e.Message, isSuccess = false, redirectControl = "Storage", redirectAction = "AddItem" });
            }
        }
        //___________Action to display the ListItem view____________________________________________________
        public ActionResult ListItems()
        {
            var departments = StorageManager.GetDepartments();
            ViewBag.Departments = new SelectList(departments, "Rowkey", "DepartmentName");
            var items = StorageManager.GetItems();
            StorageManager.DepartmentInUse = departments.First().RowKey;
            items = items.Where(x => x.PartitionKey == StorageManager.DepartmentInUse).ToList();
            return View(items);
        }
        //___________Action to display the Update view______________________________
        public ActionResult Update(String id)
        {
            var departments = StorageManager.GetDepartments();
            ViewBag.Departments = new SelectList(departments, "Rowkey", "DepartmentName");
            Item item = StorageManager.GetItem(id);
            CloudBlobContainer container = StorageManager.GetCloudBlobContainer(StorageManager.DepartmentInUse);
            var blobs = container.ListBlobs();
            List<Item> blobUrls = new List<Item>();
            foreach (var blob in blobs)
            {
                blobUrls.Add(new Item { ImgURL = blob.StorageUri.PrimaryUri.ToString() });
            }
            ParentModel pm = new ParentModel() { Item = item, Items = blobUrls };

            return View(pm);
        }
        //___________[HttpPost]Action to accept the updated item info from the AddItem view and process______________________________
        [HttpPost]
        public ActionResult Update(ParentModel pm, String ImgUrl, String Departments, String Price)
        {
            double price;
            if (!double.TryParse(Price, out price))
            {
                Price = Price.Replace('.', ',');
            }
            try
            {
                price = double.Parse(Price);
                Item item = new Item(Departments, pm.Item.ItemName) { ImgURL = ImgUrl, ItemName = pm.Item.ItemName, Price = price, Description = pm.Item.Description, ETag = "*" };
                Item exists = StorageManager.GetItem(item.RowKey);
                if (exists != null)
                {
                    StorageManager.UpdateItem(item);
                }
                else
                {
                    StorageManager.DeleteItem(pm.Item);
                    StorageManager.AddItem(item);
                }
                return RedirectToAction("Feedback", "Home", new { message = item.ItemName + " Successfully Updated", isSuccess = true, redirectControl = "Storage", redirectAction = "ListItems" });
            }
            catch (Exception e)
            {
                return RedirectToAction("Feedback", "Home", new { message = "Failed to add Item. " + e.Message, isSuccess = false, redirectControl = "Storage", redirectAction = "ListItems" });
            }
        }
        //___________Action to display the Delete Item view______________________________
        public ActionResult Delete(String id)
        {
            Item item = StorageManager.GetItem(id);
            return View(item);
        }
        //___________[HttpPost]Action to delete specified item from the Delete Item view and process______________________________
        [HttpPost]
        public ActionResult Delete(Item item)
        {
            try
            {
                StorageManager.DeleteItem(item);
                return RedirectToAction("Feedback", "Home", new { message = item.ItemName + " Successfully Added", isSuccess = true, redirectControl = "Storage", redirectAction = "ListItems" });
            }
            catch (Exception e)
            {
                return RedirectToAction("Feedback", "Home", new { message = "Failed to delete Item. " + e.Message, isSuccess = false, redirectControl = "Storage", redirectAction = "ListItems" });
            }
        }
        //___________Action to display the Add or remove images view______________________________
        public ActionResult AddRemoveImages(String id)
        {
            var departments = StorageManager.GetDepartments();
            CloudBlobContainer container;
            if (id != null)
            {
                ViewBag.Departments = new SelectList(departments, "Rowkey", "DepartmentName", id);
                container = StorageManager.GetCloudBlobContainer(id);
            }
            else
            {
                ViewBag.Departments = new SelectList(departments, "Rowkey", "DepartmentName");
                StorageManager.DepartmentInUse = departments.First().DepartmentName;
                container = StorageManager.GetCloudBlobContainer(StorageManager.DepartmentInUse);
            }

            var blobs = container.ListBlobs();
            List<Item> blobUrls = new List<Item>();
            foreach (var blob in blobs)
            {
                blobUrls.Add(new Item { ImgURL = blob.StorageUri.PrimaryUri.ToString() });
            }
            return View(blobUrls);
        }
        //___________Action to Delete image blob from image info from Add or remove images view______________________________
        [HttpPost]
        public ActionResult DeleteImage(String ImgUrl, String Departments)
        {
            CloudBlobContainer blobContainer = StorageManager.GetCloudBlobContainer(Departments);
            Uri uri = new Uri(ImgUrl);
            string filename = "";
            filename = System.IO.Path.GetFileName(uri.LocalPath);

            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(filename);
            blob.DeleteIfExists();
            return RedirectToAction("AddRemoveImages", new { id = Departments });
        }
        //___________Action to Upload images blob from drag and drop div______________________________
        [HttpPost]
        public ActionResult UploadImage(IEnumerable<HttpPostedFileBase> images)
        {
            foreach (var image in images)
            {
                CloudBlobContainer container = StorageManager.GetCloudBlobContainer(StorageManager.DepartmentInUse);
                CloudBlockBlob blob = container.GetBlockBlobReference(image.FileName);
                blob.UploadFromStream(image.InputStream);
            }
            return Json("Images uploaded successfully");
        }
        //___________PartialView to populate Target div on change of department selection______________________________
        public PartialViewResult ImagesByDepartment(String id)
        {
            StorageManager.DepartmentInUse = id;
            CloudBlobContainer container = StorageManager.GetCloudBlobContainer(id);
            var blobs = container.ListBlobs();
            List<Item> blobUrls = new List<Item>();
            ParentModel pm = new ParentModel();
            foreach (var blob in blobs)
            {
                blobUrls.Add(new Item { ImgURL = blob.StorageUri.PrimaryUri.ToString() });
            }
            pm.Items = blobUrls;
            return PartialView(pm);
        }
        //___________PartialView to populate Target div on change of department selection______________________________
        public PartialViewResult ItemByDepartment(String id)
        {
            var items = StorageManager.GetItems();
            StorageManager.DepartmentInUse = id;
            items = items.Where(x => x.PartitionKey == StorageManager.DepartmentInUse).ToList();

            return PartialView(items);
        }

    }
}