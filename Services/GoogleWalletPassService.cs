using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Walletobjects.v1;
using Google.Apis.Walletobjects.v1.Data;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRBusinessCard.Models;

namespace QRBusinessCard.Services;

/// <summary>
/// Class to help manage the creation and update of Google Wallet pass classes
/// </summary>
class GoogleWalletPassService
{
  /// <summary>
  /// Path to service account key file from Google Cloud Console. Environment
  /// variable: GOOGLE_APPLICATION_CREDENTIALS.
  /// </summary>
  public static string keyFilePath;

  /// <summary>
  /// Service account credentials for Google Wallet APIs
  /// </summary>
  public static ServiceAccountCredential credentials;

  /// <summary>
  /// Google Wallet service client
  /// </summary>
  public static WalletobjectsService service;

  public GoogleWalletPassService()
  {
    /// TODO: See if this can be changed to be retrieved from KeyVault
    keyFilePath = Environment.GetEnvironmentVariable(
        "GOOGLE_APPLICATION_CREDENTIALS") ?? "/path/to/key.json";

    Auth();
  }
  
  /// <summary>
  /// Create authenticated service client using a service account file.
  /// </summary>
  public void Auth()
  {
    credentials = (ServiceAccountCredential)GoogleCredential
        .FromFile(keyFilePath)
        .CreateScoped(new List<string>
        {
          WalletobjectsService.ScopeConstants.WalletObjectIssuer
        })
        .UnderlyingCredential;

    service = new WalletobjectsService(
        new BaseClientService.Initializer()
        {
          HttpClientInitializer = credentials
        });
  }
  
  /// <summary>
  /// Create a class, if it does not already exist.
  /// </summary>
  /// <param name="issuerId">The issuer ID being used for this request.</param>
  /// <param name="classSuffix">Developer-defined unique ID for this pass class.</param>
  /// <returns>The pass class ID: "{issuerId}.{classSuffix}"</returns>
  public GenericClass CreateClass(string issuerId, string classSuffix)
  {
    // Check if the class exists
    Stream responseStream = service.Genericclass
        .Get($"{issuerId}.{classSuffix}")
        .ExecuteAsStream();

    StreamReader responseReader = new StreamReader(responseStream);
    JObject jsonResponse = JObject.Parse(responseReader.ReadToEnd());

    if (!jsonResponse.ContainsKey("error"))
    {
      Console.WriteLine($"Class {issuerId}.{classSuffix} already exists!");
      //serialize the response to a class object
      return JsonConvert.DeserializeObject<GenericClass>(jsonResponse.ToString());
    }
    else if (jsonResponse["error"].Value<int>("code") != 404)
    {
      // Something else went wrong...
      throw new Exception(jsonResponse["error"].ToString());
    }

    // See link below for more information on required properties
    // https://developers.google.com/wallet/generic/rest/v1/genericclass
    GenericClass newClass = new GenericClass
    {
      Id = $"{issuerId}.{classSuffix}",
      ClassTemplateInfo = new ClassTemplateInfo {
        CardTemplateOverride = new CardTemplateOverride {
          CardRowTemplateInfos = new List<CardRowTemplateInfo> {
            new CardRowTemplateInfo {
              OneItem = new CardRowOneItem {
                Item = new TemplateItem {
                  FirstValue = new FieldSelector {
                    Fields = new List<FieldReference> {
                      new FieldReference {
                        FieldPath = "object.textModulesData['job_title']"
                      }
                    }
                  }
                }
              }
            },
            new CardRowTemplateInfo {
              OneItem = new CardRowOneItem {
                Item = new TemplateItem {
                  FirstValue = new FieldSelector {
                    Fields = new List<FieldReference> {
                      new FieldReference {
                        FieldPath = "object.textModulesData['email']"
                      }
                    }
                  }
                }
              }
            },
            new CardRowTemplateInfo {
              OneItem = new CardRowOneItem {
                Item = new TemplateItem {
                  FirstValue = new FieldSelector {
                    Fields = new List<FieldReference> {
                      new FieldReference {
                        FieldPath = "object.textModulesData['phone']"
                      }
                    }
                  }
                }
              }
            }
          }
        },
      }
    };

    // Add class
    Console.WriteLine("Inserting class...");
    var insertRequest = service.Genericclass
        .Insert(newClass);
    responseStream = insertRequest.ExecuteAsStream();
    responseReader = new StreamReader(responseStream);

    // Check the response for errors
    jsonResponse = JObject.Parse(responseReader.ReadToEnd());
    if (jsonResponse.ContainsKey("error"))
    {
      throw new Exception(jsonResponse["error"].ToString());
    }

    return newClass;
  }
  
  /// <summary>
  /// Create a new Generic object.
  /// </summary>
  /// <param name="issuerId">The issuer ID being used for this request.</param>
  /// <param name="classSuffix">Developer-defined unique ID for this pass class.</param>
  /// <param name="objectSuffix">Developer-defined unique ID for this pass object.</param>
  /// <param name="contactInfo">Contact information to display on the pass object.</param>
  /// <returns>The new pass object, with ID: "{issuerId}.{objectSuffix}"</returns>
  private GenericObject CreateObject(string issuerId, string classSuffix, string objectSuffix, ContactInfo contactInfo)
  {
    // See link below for more information on required properties
    // https://developers.google.com/wallet/generic/rest/v1/genericobject
    GenericObject localObject = new GenericObject
    {
      Id = $"{issuerId}.{objectSuffix}",
      ClassId = $"{issuerId}.{classSuffix}",
      State = "ACTIVE",
      Logo = new Image
      {
        SourceUri = new ImageUri
        {
          Uri = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/44/Microsoft_logo.svg/240px-Microsoft_logo.svg.png"
        },
        ContentDescription = new LocalizedString
        {
          DefaultValue = new TranslatedString
          {
            Language = "en-US",
            Value = "Microsoft logo"
          }
        },
      },
      CardTitle = new LocalizedString
      {
        DefaultValue = new TranslatedString
        {
          Language = "en-US",
          Value = "Bussiness Card"
        }
      },
      Subheader = new LocalizedString
      {
        DefaultValue = new TranslatedString
        {
          Language = "en-US",
          Value = contactInfo.Company
        }
      },
      Header = new LocalizedString
      {
        DefaultValue = new TranslatedString
        {
          Language = "en-US",
          Value = $"{contactInfo.FirstName} {contactInfo.LastName}"
        }
      },
      TextModulesData = new List<TextModuleData>
        {
          new TextModuleData
          {
            Id = "job_title",
            Header = "Job Title",
            Body = contactInfo.JobTitle
          },
          new TextModuleData
          {
            Id = "email",
            Header = "email",
            Body = contactInfo.Email
          },
          new TextModuleData
          {
            Id = "phone",
            Header = "Phone",
            Body = contactInfo.Phone,
          },
        },
      Barcode = new Barcode
      {
        Type = "QR_CODE",
        Value = QRCodeService.BuildVCard(contactInfo),
      },
      HexBackgroundColor = "#4285f4"
    };

    // check if an existing wallet pass exists remotely
    Stream responseStream = service.Genericobject
        .Get($"{issuerId}.{objectSuffix}")
        .ExecuteAsStream();

    // Check the response
    StreamReader responseReader = new StreamReader(responseStream);
    JObject jsonResponse = JObject.Parse(responseReader.ReadToEnd());
    if (!jsonResponse.ContainsKey("error"))
    {
      Console.WriteLine("Updating existing object");
      // Update the existing pass
      responseStream = service.Genericobject
        .Update(localObject, $"{issuerId}.{objectSuffix}")
        .ExecuteAsStream();
    }
    else if (jsonResponse["error"].Value<int>("code") == 404)
    {
      Console.WriteLine("Creating new object");
      // Create a new pass
      responseStream = service.Genericobject
        .Insert(localObject)
        .ExecuteAsStream();
    }
    else
    {
      // Something else went wrong
      throw new Exception(jsonResponse["error"].ToString());
    }

    // read the response
    responseReader = new StreamReader(responseStream);
    jsonResponse = JObject.Parse(responseReader.ReadToEnd());
    if (jsonResponse.ContainsKey("error")) {
      throw new Exception(jsonResponse["error"].ToString());
    }
    return localObject;
  }

  /// <summary>
  /// Get the Add To Wallet link for a pass object.
  /// </summary>
  /// <param name="issuerId">The issuer ID being used for this request.</param>
  /// <param name="classSuffix">Developer-defined unique ID for this pass class.</param>
  /// <param name="objectSuffix">Developer-defined unique ID for this pass object.</param>
  /// <param name="contactInfo">Contact information to display on the pass object.</param>
  /// <returns>An "Add to Google Wallet" link.</returns>
  public string GetAddToWalletLink(string issuerId, string classSuffix, string objectSuffix, ContactInfo contactInfo)
  {
    // ensure that the class exists
    CreateClass(issuerId, classSuffix);

    // create local generic object for later use
    GenericObject localObject = CreateObject(issuerId, classSuffix, objectSuffix, contactInfo);

    // Ignore null values when serializing to/from JSON
    JsonSerializerSettings excludeNulls = new JsonSerializerSettings()
    {
      NullValueHandling = NullValueHandling.Ignore
    };
    
    // Create JSON representations of the class and object
    // JObject serializedClass = JObject.Parse(
    //      JsonConvert.SerializeObject(newClass, excludeNulls));
    JObject serializedObject = JObject.Parse(
        JsonConvert.SerializeObject(localObject, excludeNulls));

    // Create the JWT as a JSON object
    JObject jwtPayload = JObject.Parse(JsonConvert.SerializeObject(new
    {
      iss = credentials.Id,
      aud = "google",
      origins = new List<string>
      {
        "www.example.com"
      },
      typ = "savetowallet",
      payload = JObject.Parse(JsonConvert.SerializeObject(new
      {
        // The listed classes and objects will be created
        // when the user saves the pass to their wallet
        // genericClasses = new List<JObject>
        // {
        //   serializedClass
        // },
        genericObjects = new List<JObject>
        {
          serializedObject
        }
      }))
    }));

    // Deserialize into a JwtPayload
    JwtPayload claims = JwtPayload.Deserialize(jwtPayload.ToString());

    // The service account credentials are used to sign the JWT
    RsaSecurityKey key = new RsaSecurityKey(credentials.Key);
    SigningCredentials signingCredentials = new SigningCredentials(
        key, SecurityAlgorithms.RsaSha256);
    JwtSecurityToken jwt = new JwtSecurityToken(
        new JwtHeader(signingCredentials), claims);
    string token = new JwtSecurityTokenHandler().WriteToken(jwt);

    return $"https://pay.google.com/gp/v/save/{token}";
  }
}