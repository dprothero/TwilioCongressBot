using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Twilio.Mvc;
using Twilio.TwiML;
using Twilio.TwiML.Mvc;
using TwilioCongressBot.Web.Models;

namespace TwilioCongressBot.Web.Controllers
{
  public class LegislatorController : Controller
  {
    private string _congress_api_url_template = "https://congress.api.sunlightfoundation.com/legislators/locate?zip={0}&apikey={1}";

    private IDictionary<string, string> labelDict = new Dictionary<string, string>()
    {
      { "house", "House Representative" },
      { "senate", "Senator" }
    };

    [HttpPost]
    public ActionResult Index(SmsRequest request)
    {
      var response = new TwilioResponse();

      var zip = getValidZip(request, response);
      if(zip == null)
      {
        response.Message("Please text your ZIP code.");
      }
      else
      {
        var legislators = getLegislators(zip);
        if (legislators == null)
        {
          response.Message("Error retrieving results for " + zip);
        }
        else
        {
          foreach(var legislator in legislators.results)
          {
            response.Message("Your " + labelDict[legislator.chamber] + ": " +
              getFullName(legislator) + " - " + legislator.phone + " - " +
              legislator.oc_email);
          }
        }
      }

      return new TwiMLResult(response);
    }

    private CongressApiResult getLegislators(string zip)
    {
      var url = string.Format(_congress_api_url_template, zip, ConfigurationManager.AppSettings["CongressApiKey"]);
      var client = new RestClient(url);
      var request = new RestRequest(Method.GET);
      var response = client.Execute(request);
      if (response.ResponseStatus != ResponseStatus.Completed || 
        response.StatusCode != System.Net.HttpStatusCode.OK)
      {
        return null;
      }
      return JsonConvert.DeserializeObject<CongressApiResult>(response.Content);
    }

    private string getValidZip(SmsRequest request, TwilioResponse response)
    {
      if (isValidZip(request.Body)) return request.Body;
      if (isValidZip(request.FromZip))
      {
        response.Message("Text your ZIP code for more accurate results. We're guessing " + request.FromZip);
        return request.FromZip;
      }
      return null;
    }

    private Regex _zipValidator = new Regex(@"^\d{5}$");
    private bool isValidZip(string zipToCheck)
    {
      return _zipValidator.IsMatch(zipToCheck);
    }

    private string getFullName(Legislator legislator)
    {
      return (legislator.first_name + " " +
              legislator.middle_name + " " +
              legislator.last_name).Replace("  ", " ");
    }
  }
}