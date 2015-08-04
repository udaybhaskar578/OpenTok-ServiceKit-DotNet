using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using OpenTokSDK;
using ServiceKit_DotNet.data;

/***************************************************************************
 * Source: Service Kit - PHP ( https://tokbox.com/developer/starter-kits/)
 * Modified by : Sai Uday Bhaskar Mudivarty
 * Uses Backbone.Js,Lodash.js,OpenTok SDK
 * All the routings are defined in Global.asax file
 ***************************************************************************/

namespace ServiceKit_DotNet.Controllers
{
    public class HomeController : Controller
    {
        

        // Saving the customer data into table storage
        [HttpPost]
        public ActionResult HelpSession()
        {
           
                string errorMessage = null;
                var customerName = Request.Params["customer_name"];
                var  problemText = Request.Params["problem_text"];
                Debug.WriteLine(customerName + problemText);
                // Validation for the text fields
                if (customerName.IsEmpty() || problemText.IsEmpty())
                {
                    errorMessage = "The fields customer_name and problem_text are required.";
                }
                if (customerName.Length > 50)
                {
                    errorMessage = "The field customer_name is too long";
                }
                if (problemText.Length > 200)
                {
                    errorMessage = "The field problem_text is too long";
                }
                if (!(errorMessage.IsEmpty()))
                {
                    Response.SetStatus(400);
                    return Json(errorMessage);
                }
                var session = Settings.Tk.CreateSession();
                var model = new HelpSessionDataType
                {
                    PartitionKey = session.Id,
                    ProblemText = problemText,
                    CustomerName = customerName,
                    SessionId = session.Id
                };
                HelpSessionData.InsertOrUpdate(model);
                var responseData =
                    new {apiKey = Settings.Tk.ApiKey, sessionId = session.Id, token = session.GenerateToken()};
                Response.ContentType = "application/json";
                return Json(responseData, JsonRequestBehavior.AllowGet);
        }
      
        // Inserting the session Id of the customer into a queue for the next available representative
        [HttpPost]
        public ActionResult HelpQueue()
        {
            //Requesting the Session Id
            string sessionId = Request.Params["session_id"];
            // Get the details of the HelpSession from Table Storage
            var model = HelpSessionData.GetHelpSession(sessionId);
            if (model == null)
            {
                Response.SetStatus(400);
            }
            //Insert the session Id into the CloudQueue
            HelpSessionQueue.Insert(sessionId);
            var responseData = new {queueId = sessionId};
            return Json(responseData, JsonRequestBehavior.AllowGet);
        }

        // Deleting a particular queueId from the queue when the user cancels the chat session or closes the window
        [HttpPost]
        public ActionResult DeleteQueueId()
        {
            // Get all the messages from the queue
            var helpSessionKey = HelpSessionQueue.GetMessages();
            // Get the current queueId ( To delete a session from the queue when the user closes the window or clicks on cancel
            string sessionId = Request.Params["session_id"];
            foreach (CloudQueueMessage cqm in helpSessionKey)
            {
                if (cqm.AsString == sessionId)
                {
                    // Deleting that message from the queue
                    HelpSessionQueue.DeleteMessage(cqm);
                }
            }
            Response.SetStatus(HttpStatusCode.NoContent);
            return null;

        }

        
        //Gets the data of the customer to the representative
        [HttpPost]
        public ActionResult HelpDequeue()
        {
            CloudQueueMessage helpSessionKey = HelpSessionQueue.GetMessage();
            var model = new HelpSessionDataType();
            if(helpSessionKey == null)
            {
                Response.SetStatus(HttpStatusCode.NoContent);
                return null;
            }
            model = GetSession(helpSessionKey.AsString);
            HelpSessionQueue.DeleteMessage(helpSessionKey);
            var responseData = new
            {
                apiKey = Settings.Tk.ApiKey,
                sessionId = model.SessionId,
                token = Settings.Tk.GenerateToken(model.SessionId),
                customerName = model.CustomerName,
                problemText = model.ProblemText
            };
            return Json(responseData, JsonRequestBehavior.AllowGet);
  
        }

        // Helper function 
        public HelpSessionDataType GetSession(string helpSession)
        {
            var model = HelpSessionData.GetHelpSession(helpSession);
            if (model == null)
            {
                return null;
            }
            return model;

        }
        public ActionResult Rep()
        {
            return View();
        }


        //
        // GET: /Home/
        public ActionResult Index()
        {

            return View();
        }
        
	}
}