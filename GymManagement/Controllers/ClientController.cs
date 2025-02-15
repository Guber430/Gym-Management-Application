using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GymManagement.Data;
using GymManagement.Models;
using Microsoft.EntityFrameworkCore.Storage;
using System.Numerics;
using GymManagement.CustomControllers;
using GymManagement.Utilities;
using GymManagement.ViewModels;
using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.AspNetCore.Authorization;

namespace GymManagement.Controllers
{
    [Authorize]
    public class ClientController : ElephantController
    {
        //For sending email
        private readonly IMyEmailSender _emailSender;
        private readonly GymContext _context;

        public ClientController(GymContext context, IMyEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // GET: Clients
        [Authorize(Roles = "Client,Staff,Supervisor,Admin")]
        public async Task<IActionResult> Index(int? page, int? pageSizeID)
        {
            var clients = _context.Clients
                .Include(c => c.MembershipType)
                .Include(p => p.ClientThumbnail)
                .AsNoTracking();

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, ControllerName());
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Client>.CreateAsync(clients.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Clients/Details/5
        [Authorize(Roles = "Client,Staff,Supervisor,Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.MembershipType)
                .Include(p => p.ClientPhoto)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // GET: Clients/Create
        [Authorize(Roles = "Staff,Supervisor,Admin")]
        public IActionResult Create()
        {
            PopulateDropDownLists();
            return View();
        }

        // POST: Clients/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,MembershipNumber,FirstName,MiddleName,LastName,Phone," +
            "Email,DOB,PostalCode,HealthCondition,Notes,MembershipStartDate,MembershipEndDate," +
            "MembershipFee,FeePaid,MembershipTypeID")] Client client, IFormFile? thePicture)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await AddPicture(client, thePicture);
                    _context.Add(client);
                    await _context.SaveChangesAsync();
                    //Send on to add Workouts
                    return RedirectToAction("Index", "ClientWorkout", new { ClientID = client.ID });
                }
            }
            catch (RetryLimitExceededException /* dex */)//This is a Transaction in the Database!
            {
                ModelState.AddModelError("", "Unable to save changes after multiple attempts. " +
                    "Try again, and if the problem persists, see your system administrator.");
            }
            catch (DbUpdateException dex)
            {
                string message = dex.GetBaseException().Message;
                if (message.Contains("UNIQUE") && message.Contains("Clients.MembershipNumber"))
                {
                    ModelState.AddModelError("MembershipNumber", "Unable to save changes. Remember, " +
                        "you cannot have duplicate Membership Numbers.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }

            PopulateDropDownLists(client);
            return View(client);
        }

        // GET: Clients/Edit/5
        [Authorize(Roles = "Client,Staff,Supervisor,Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.MembershipType)
                .Include(p => p.ClientPhoto)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (client == null)
            {
                return NotFound();
            }
            PopulateDropDownLists(client);
            return View(client);
        }

        // POST: Clients/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion, string? chkRemoveImage, IFormFile? thePicture)
        {
            //Get the Client to update
            var clientToUpdate = await _context.Clients
                .Include(c => c.MembershipType)
                .Include(p => p.ClientPhoto)
                .FirstOrDefaultAsync(m => m.ID == id);
            //Check that you got it or exit with a not found error
            if (clientToUpdate == null)
            {
                return NotFound();
            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(clientToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Client>(clientToUpdate, "",
                p => p.MembershipNumber, p => p.FirstName, p => p.MiddleName, p => p.LastName, 
                p => p.Phone, p => p.Email, p => p.DOB,
                p => p.PostalCode,  p => p.HealthCondition, p => p.Notes, 
                p => p.MembershipStartDate, p => p.MembershipEndDate, p => p.MembershipFee, 
                p => p.FeePaid, p => p.MembershipTypeID ))
            {
                try
                {
                    //For the image
                    if (chkRemoveImage != null)
                    {
                        //If we are just deleting the two versions of the photo, we need to make sure the Change Tracker knows
                        //about them both so go get the Thumbnail since we did not include it.
                        clientToUpdate.ClientThumbnail = _context.ClientThumbnails.Where(p => p.ClientID == clientToUpdate.ID).FirstOrDefault();
                        //Then, setting them to null will cause them to be deleted from the database.
                        clientToUpdate.ClientPhoto = null;
                        clientToUpdate.ClientThumbnail = null;
                    }
                    else
                    {
                        await AddPicture(clientToUpdate, thePicture);
                    }

                    _context.Update(clientToUpdate);
                    await _context.SaveChangesAsync();
                    var returnUrl = ViewData["returnURL"]?.ToString();
                    if (string.IsNullOrEmpty(returnUrl))
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    return Redirect(returnUrl);
                }
                catch (DbUpdateConcurrencyException ex)// Added for concurrency
                {
                    var exceptionEntry = ex.Entries.Single();
                    var clientValues = (Client)exceptionEntry.Entity;
                    var databaseEntry = exceptionEntry.GetDatabaseValues();
                    if (databaseEntry == null)
                    {
                        ModelState.AddModelError("",
                            "Unable to save changes. The Client was deleted by another user.");
                    }
                    else
                    {
                        var databaseValues = (Client)databaseEntry.ToObject();

                        if (databaseValues.MembershipNumber != clientValues.MembershipNumber)
                            ModelState.AddModelError("MembershipNumber", "Current value: "
                                + databaseValues.MembershipNumber);
                        if (databaseValues.FirstName != clientValues.FirstName)
                            ModelState.AddModelError("FirstName", "Current value: "
                                + databaseValues.FirstName);
                        if (databaseValues.MiddleName != clientValues.MiddleName)
                            ModelState.AddModelError("MiddleName", "Current value: "
                                + databaseValues.MiddleName);
                        if (databaseValues.LastName != clientValues.LastName)
                            ModelState.AddModelError("LastName", "Current value: "
                                + databaseValues.LastName);
                        if (databaseValues.Phone != clientValues.Phone)
                            ModelState.AddModelError("Phone", "Current value: "
                                + databaseValues.PhoneFormatted);
                        if (databaseValues.Email != clientValues.Email)
                            ModelState.AddModelError("Email", "Current value: "
                                + databaseValues.Email);
                        if (databaseValues.DOB != clientValues.DOB)
                            ModelState.AddModelError("DOB", "Current value: "
                                + String.Format("{0:d}", databaseValues.DOB));
                        if (databaseValues.PostalCode != clientValues.PostalCode)
                            ModelState.AddModelError("PostalCode", "Current value: "
                                + databaseValues.PostalCode);
                        if (databaseValues.HealthCondition != clientValues.HealthCondition)
                            ModelState.AddModelError("HealthCondition", "Current value: "
                                + databaseValues.HealthCondition);
                        if (databaseValues.Notes != clientValues.Notes)
                            ModelState.AddModelError("Notes", "Current value: "
                                + databaseValues.Notes);
                        if (databaseValues.MembershipStartDate != clientValues.MembershipStartDate)
                            ModelState.AddModelError("MembershipStartDate", "Current value: "
                                + String.Format("{0:d}", databaseValues.MembershipStartDate));
                        if (databaseValues.MembershipEndDate != clientValues.MembershipEndDate)
                            ModelState.AddModelError("MembershipEndDate", "Current value: "
                                + String.Format("{0:d}", databaseValues.MembershipEndDate));
                        if (databaseValues.MembershipFee != clientValues.MembershipFee)
                            ModelState.AddModelError("MembershipFee", "Current value: "
                                + String.Format("{0:c}", databaseValues.MembershipFee));
                        if (databaseValues.FeePaid != clientValues.FeePaid)
                            ModelState.AddModelError("FeePaid", "Current value: "
                                + databaseValues.FeePaid);


                        //For the foreign key, we need to go to the database to get the information to show
                        if (databaseValues.MembershipTypeID != clientValues.MembershipTypeID)
                        {
                            MembershipType? databaseMembershipType = await _context.MembershipTypes
                                .FirstOrDefaultAsync(i => i.ID == databaseValues.MembershipTypeID);
                            ModelState.AddModelError("MembershipTypeID", $"Current value: {databaseMembershipType?.Summary}");
                        }
                        
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                                + "was modified by another user after you received your values. The "
                                + "edit operation was canceled and the current values in the database "
                                + "have been displayed. If you still want to save your version of this record, click "
                                + "the Save button again. Otherwise click the 'Back to the "
                                + ViewData["ControllerFriendlyName"] + " List' hyperlink.");

                        //Final steps before redisplaying: Update RowVersion from the Database
                        //and remove the RowVersion error from the ModelState
                        clientToUpdate.RowVersion = databaseValues.RowVersion ?? Array.Empty<byte>();
                        ModelState.Remove("RowVersion");
                    }
                }
                catch (RetryLimitExceededException /* dex */)//This is a Transaction in the Database!
                {
                    ModelState.AddModelError("", "Unable to save changes after multiple attempts. " +
                        "Try again, and if the problem persists, see your system administrator.");
                }
                catch (DbUpdateException dex)
                {
                    string message = dex.GetBaseException().Message;
                    if (message.Contains("UNIQUE") && message.Contains("Clients.MembershipNumber"))
                    {
                        ModelState.AddModelError("MembershipNumber", "Unable to save changes. Remember, " +
                            "you cannot have duplicate Membership Numbers.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                    }
                }
            }
            PopulateDropDownLists(clientToUpdate);
            return View(clientToUpdate);
        }

        // GET: Clients/Delete/5
        [Authorize(Roles = "Supervisor,Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.MembershipType)
                .Include(p => p.ClientPhoto)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // POST: Clients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _context.Clients
                .Include(c => c.MembershipType)
                .Include(p => p.ClientPhoto)
                .FirstOrDefaultAsync(m => m.ID == id);
            try
            {
                if (client != null)
                {
                    _context.Clients.Remove(client);
                }

                await _context.SaveChangesAsync();
                var returnUrl = ViewData["returnURL"]?.ToString();
                if (string.IsNullOrEmpty(returnUrl))
                {
                    return RedirectToAction(nameof(Index));
                }
                return Redirect(returnUrl);
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("FOREIGN KEY constraint failed"))
                {
                    ModelState.AddModelError("", "Unable to Delete Client. Remember, you cannot delete " +
                        "a Client that is enrolled in any Group Classes.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the " +
                        "problem persists see your system administrator.");
                }
            }
            return View(client);
        }

        public async Task<IActionResult> MembershipTypeSummary(int? page, int? pageSizeID)
        {
            var sumQ = _context.Clients.Include(c=>c.MembershipType)
                .Where(a => a.FeePaid)
                .GroupBy(static a => new { a.MembershipType.Type } )
                .Select(grp => new MembershipTypeSummaryVM
                {
                    Membership_Type = grp.Key.Type,
                    Number_Of_Clients = grp.Count(),
                    Average_Fee = grp.Average(a => a.MembershipFee),
                    Highest_Fee = grp.Max(a => a.MembershipFee),
                    Lowest_Fee = grp.Min(a => a.MembershipFee),
                    Total_Fees = grp.Sum(a => a.MembershipFee),
                });

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "MembershipTypeSummary");//Remember for this View
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<MembershipTypeSummaryVM>.CreateAsync(sumQ.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        public IActionResult MembershipTypeReport()
        {
            var membershipTypes = _context.Clients.Include(c => c.MembershipType)
                        .Where(a => a.FeePaid)
                        .GroupBy(static a => new { a.MembershipType.Type })
                        .Select(grp => new MembershipTypeSummaryVM
                        {
                            Membership_Type = grp.Key.Type,
                            Number_Of_Clients = grp.Count(),
                            Average_Fee = grp.Average(a => a.MembershipFee),
                            Highest_Fee = grp.Max(a => a.MembershipFee),
                            Lowest_Fee = grp.Min(a => a.MembershipFee),
                            Total_Fees = grp.Sum(a => a.MembershipFee),
                        });
            //How many rows?
            int numRows = membershipTypes.Count();

            if (numRows > 0) //We have data
            {
                //Create a new spreadsheet from scratch.
                using (ExcelPackage excel = new ExcelPackage())
                {

                    var workSheet = excel.Workbook.Worksheets.Add("MembershipTypes");

                    //Note: Cells[row, column]
                    workSheet.Cells[3, 1].LoadFromCollection(membershipTypes, true);

                    //Style column for currency
                    workSheet.Column(3).Style.Numberformat.Format = "###,##0.00";
                    workSheet.Column(4).Style.Numberformat.Format = "###,##0.00";
                    workSheet.Column(5).Style.Numberformat.Format = "###,##0.00";
                    workSheet.Column(6).Style.Numberformat.Format = "###,##0.00";

                    //Note: You can define a BLOCK of cells: Cells[startRow, startColumn, endRow, endColumn]
                    //Make Date and Patient Bold
                    workSheet.Cells[4, 1, numRows + 3, 2].Style.Font.Bold = true;

                    //Note: these are fine if you are only 'doing' one thing to the range of cells.
                    //Otherwise you should USE a range object for efficiency
                    using (ExcelRange totalfees = workSheet.Cells[numRows + 4, 1])//
                    {
                        totalfees.Value = "Totals:";
                        totalfees.Style.Font.Bold = true;
                    }
                    using (ExcelRange totalfees = workSheet.Cells[numRows + 4, 2])//
                    {
                        totalfees.Formula = "Sum(" + workSheet.Cells[4, 2].Address + ":" + workSheet.Cells[numRows + 3, 2].Address + ")";
                        totalfees.Style.Font.Bold = true;
                        totalfees.Style.Numberformat.Format = "###,##0";
                    }
                    using (ExcelRange totalfees = workSheet.Cells[numRows + 4, 6])//
                    {
                        totalfees.Formula = "Sum(" + workSheet.Cells[4, 6].Address + ":" + workSheet.Cells[numRows + 3, 6].Address + ")";
                        totalfees.Style.Font.Bold = true;
                        totalfees.Style.Numberformat.Format = "$###,##0.00";
                    }

                    //Set Style and backgound colour of headings
                    using (ExcelRange headings = workSheet.Cells[3, 1, 3, 6])
                    {
                        headings.Style.Font.Bold = true;
                        var fill = headings.Style.Fill;
                        fill.PatternType = ExcelFillStyle.Solid;
                        fill.BackgroundColor.SetColor(Color.LightBlue);
                    }

                    //Autofit columns
                    workSheet.Cells.AutoFitColumns();
                    //Note: You can manually set width of columns as well
                    //workSheet.Column(7).Width = 10;

                    //Add a title and timestamp at the top of the report
                    workSheet.Cells[1, 1].Value = "Membership Type Summary";
                    using (ExcelRange Rng = workSheet.Cells[1, 1, 1, 6])
                    {
                        Rng.Merge = true; //Merge columns start and end range
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 18;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //Since the time zone where the server is running can be different, adjust to 
                    //Local for us.
                    DateTime utcDate = DateTime.UtcNow;
                    TimeZoneInfo esTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    DateTime localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, esTimeZone);
                    using (ExcelRange Rng = workSheet.Cells[2, 6])
                    {
                        Rng.Value = "Created: " + localDate.ToShortTimeString() + " on " +
                            localDate.ToShortDateString();
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 12;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    //Ok, time to download the Excel

                    try
                    {
                        Byte[] theData = excel.GetAsByteArray();
                        string filename = "MembershipTypeReport.xlsx";
                        string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        return File(theData, mimeType, filename);
                    }
                    catch (Exception)
                    {
                        return BadRequest("Could not build and download the file.");
                    }
                }
            }
            return NotFound("No data.");
        }


        private SelectList MembershipTypeList(int? selectedId)
        {
            return new SelectList(_context.MembershipTypes
                .OrderBy(m=>m.Type), "ID", "Type", selectedId);
        }

        private void PopulateDropDownLists(Client? client = null)
        {
            ViewData["MembershipTypeID"] = MembershipTypeList(client?.MembershipTypeID);
        }

        private async Task AddPicture(Client client, IFormFile thePicture)
        {
            //Get the picture and save it with the Client (2 sizes)
            if (thePicture != null)
            {
                string mimeType = thePicture.ContentType;
                long fileLength = thePicture.Length;
                if (!(mimeType == "" || fileLength == 0))//Looks like we have a file!!!
                {
                    if (mimeType.Contains("image"))
                    {
                        using var memoryStream = new MemoryStream();
                        await thePicture.CopyToAsync(memoryStream);
                        var pictureArray = memoryStream.ToArray();//Gives us the Byte[]

                        //Check if we are replacing or creating new
                        if (client.ClientPhoto != null)
                        {
                            //We already have pictures so just replace the Byte[]
                            client.ClientPhoto.Content = ResizeImage.ShrinkImageWebp(pictureArray, 500, 600);

                            //Get the Thumbnail so we can update it.  Remember we didn't include it
                            client.ClientThumbnail = _context.ClientThumbnails.Where(p => p.ClientID == client.ID).FirstOrDefault();
                            if (client.ClientThumbnail != null)
                            {
                                client.ClientThumbnail.Content = ResizeImage.ShrinkImageWebp(pictureArray, 75, 90);
                            }
                        }
                        else //No pictures saved so start new
                        {
                            client.ClientPhoto = new ClientPhoto
                            {
                                Content = ResizeImage.ShrinkImageWebp(pictureArray, 500, 600),
                                MimeType = "image/webp"
                            };
                            client.ClientThumbnail = new ClientThumbnail
                            {
                                Content = ResizeImage.ShrinkImageWebp(pictureArray, 75, 90),
                                MimeType = "image/webp"
                            };
                        }
                    }
                }
            }
        }

        // GET/POST: Client/Notification/5
        public async Task<IActionResult> Notification(int? id, string Subject, string emailContent)
        {
            if (id == null)
            {
                return NotFound();
            }
            Client? c = await _context.Clients.FindAsync(id);

            ViewData["id"] = id;
            ViewData["FullFormalName"] = c?.FullFormalName;

            if (string.IsNullOrEmpty(Subject) || string.IsNullOrEmpty(emailContent))
            {
                ViewData["Message"] = "You must enter both a Subject and some message Content before sending the message.";
            }
            else
            {
                int folksCount = 0;
                try
                {
                    //Send a Notice.
                    List<EmailAddress> folks = (from p in _context.Clients
                                                where p.ID == id
                                                where p.Email != null
                                                select new EmailAddress
                                                {
                                                    Name = p.Summary,
                                                    Address = p.Email
                                                }).ToList();
                    folksCount = folks.Count;
                    if (folksCount > 0)
                    {
                        var msg = new EmailMessage()
                        {
                            ToAddresses = folks,
                            Subject = Subject,
                            Content = "<p>" + emailContent + "</p><p>Please access the <strong>Niagara College</strong> web site to review.</p>"
                        };
                        await _emailSender.SendToManyAsync(msg);
                        ViewData["Message"] = "Message sent to " + folksCount + " Client"
                            + ((folksCount == 1) ? "." : "s.");
                    }
                    else
                    {
                        ViewData["Message"] = "Message NOT sent!  No Clients at the gym.";
                    }
                }
                catch (Exception ex)
                {
                    string errMsg = ex.GetBaseException().Message;
                    ViewData["Message"] = "Error: Could not send email message to the " + folksCount + " Client"
                        + ((folksCount == 1) ? "" : "s") + " at the gym.";
                }
            }
            return View();
        }

        // GET/POST: Client/MultipleNotifications/5
        public async Task<IActionResult> MultipleNotifications(string Subject, string emailContent, string[] selectedOptions)
        {
            if (string.IsNullOrEmpty(Subject) || string.IsNullOrEmpty(emailContent))
            {
                ViewData["Message"] = "You must enter both a Subject and some message Content before sending the message.";
            }
            else
            {
                int folksCount = 0;
                try
                {
                    List<EmailAddress> folks = (from c in _context.Clients
                                                where selectedOptions.Contains(c.ID.ToString()) && c.Email != null
                                                select new EmailAddress
                                                {
                                                    Name = c.Summary,
                                                    Address = c.Email
                                                }).ToList();

                    folksCount = folks.Count;

                    if (folksCount > 0)
                    {
                        var msg = new EmailMessage()
                        {
                            ToAddresses = folks,
                            Subject = Subject,
                            Content = "<p>" + emailContent + "</p><p>Please access the <strong>Niagara College</strong> website to review.</p>"
                        };
                        await _emailSender.SendToManyAsync(msg);

                        ViewData["Message"] = $"Message sent to {folksCount} Client{(folksCount == 1 ? "" : "s")}.";
                    }
                    else
                    {
                        ViewData["Message"] = "Message NOT sent! No selected clients.";
                    }
                }
                catch (Exception ex)
                {
                    string errMsg = ex.GetBaseException().Message;
                    ViewData["Message"] = $"Error: Could not send email message to the {folksCount} Client{(folksCount == 1 ? "" : "s")}.";
                }
            }

            var allClients = await _context.Clients.ToListAsync();

            ViewBag.availOpts = new MultiSelectList(
                allClients.Where(c => selectedOptions == null || !selectedOptions.Contains(c.ID.ToString())),
                "ID", "FullFormalName");

            ViewBag.selOpts = new MultiSelectList(
                allClients.Where(c => selectedOptions != null && selectedOptions.Contains(c.ID.ToString())),
                "ID", "FullFormalName");

            return View();
        }

        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.ID == id);
        }
    }
}
