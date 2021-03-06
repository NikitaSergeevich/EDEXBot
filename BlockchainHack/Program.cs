﻿﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
 using System.Security.Cryptography;
 using System.Threading.Tasks;
 using ConsoleApplication1;
 using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
 using Nethereum.RLP;
 using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace BlockchainHack
{
    public class Program
    {
        static ApiBlockChain apiBlockChain = new ApiBlockChain();
        private static string mainContractAdress = "0x6bff537405237294a5e786fda1fa8ea315e17b58";
        private static readonly TelegramBotClient Bot = new TelegramBotClient("408098530:AAGKKN-C7R2wbLcIqE0T17v7RZeNO25I8fQ");

        static Dictionary<long, Dictionary<String, List<Telegram.Bot.Types.Contact>>> contactsdict = new Dictionary<long, Dictionary<String, List<Telegram.Bot.Types.Contact>>>();
        static Dictionary<long, string> statedict = new Dictionary<long, string>();
        static Dictionary<long, string> tg_bc_dict = new Dictionary<long, string>();
        static Dictionary<long, Document> documentNameDictionary = new Dictionary<long, Document>();
        
        static List<string> blockchainpublickeys = new List<string>() 
        {
            "0x3e165d74b72bc6848329ff8ddf678ac19ec1a139",
            "0x521a2561b4eb3fda1c6af94bbf130aae23ed2765"
        };
        public static void Main(string[] args)
        {
            /*var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();*/
//            ApiBlockChain apiBlockChain = new ApiBlockChain();
            apiBlockChain.inizialWeb3();
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;
            
            var me = Bot.GetMeAsync().Result;
            
            Console.WriteLine(me.Username + "started");

            Bot.StartReceiving();
            Console.ReadLine();
            Bot.StopReceiving();
        }
        
        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Debugger.Break();
        }
        
        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received choosen inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }
        
        private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            Console.WriteLine("inlinequery");
        }
        
        // Three states: init, uploaded and set for contact group
        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var rkm = new ReplyKeyboardMarkup();
            rkm.Keyboard = 
                new KeyboardButton[][]
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("Upload"),
                    },

                    new KeyboardButton[]
                    {
                        new KeyboardButton("To Sign"),
                    },

                    new KeyboardButton[]
                    {
                        new KeyboardButton("Signed"),
                    }
                };
            rkm.OneTimeKeyboard = true;

            // Prepare all relevant data from client
            long tgid = messageEventArgs.Message.Chat.Id;
            string docname = null;
            Contact contact = null;
            if (messageEventArgs.Message.Type.Equals(MessageType.DocumentMessage)){
                docname = messageEventArgs.Message.Document.FileName;
            } else if (messageEventArgs.Message.Type.Equals(MessageType.ContactMessage)){
                contact = messageEventArgs.Message.Contact;
            }
            
            //TODO switch case
            if (messageEventArgs.Message.Type.Equals(MessageType.TextMessage) && 
                messageEventArgs.Message.Text.Equals("/start")){
                Console.WriteLine("Init for " + tgid);
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Please Select An Option", false, false, 0, rkm );
            } else if (messageEventArgs.Message.Type.Equals(MessageType.TextMessage) && 
                       messageEventArgs.Message.Text.Equals("Upload")){
                Console.WriteLine(tgid + "Want to upload");
                statedict[tgid] = "init";
                documentNameDictionary = new Dictionary<long, Document>();
                contactsdict = new Dictionary<long, Dictionary<string, List<Contact>>>();
                //Set empty / clear contact list for client with tgid
                contactsdict[tgid] = new Dictionary<string, List<Contact>>();
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Now upload file you want to sign", false, false, 0, null);
            } else if (messageEventArgs.Message.Type.Equals(MessageType.DocumentMessage) && 
                       statedict.ContainsKey(tgid) &&
                       statedict[tgid] == "init"){
                Console.WriteLine("Document received from " + tgid);
                
                //Update Data
                statedict[tgid] = "uploaded";
                documentNameDictionary[tgid] = messageEventArgs.Message.Document;
                
                //
                Dictionary<string, List<Contact>> dict = new Dictionary<string, List<Contact>>();
                dict[docname] = new List<Contact>();
                contactsdict[tgid] = dict;
                
                //Send Reply
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Document for sign: " + docname + ". Please select document signers by phone number from your contact list", false, false, 0, null );
                
            } else if (messageEventArgs.Message.Type.Equals(MessageType.ContactMessage) && 
                       statedict.ContainsKey(tgid) &&
                       (statedict[tgid] == "uploaded" || statedict[tgid] == "set"))
            {
                // Keyboard with one button for sender sign
                var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(
                    new Telegram.Bot.Types.InlineKeyboardButton[][]
                    {
                        // First row
                        new [] {
                            // First column
                            new Telegram.Bot.Types.InlineKeyboardButton("sign and send", "sendtosigncallback"),
                        },
                    }
                );
                
                // Update client state and add new contact
                statedict[tgid] = "set";
                contactsdict[tgid][documentNameDictionary[tgid].FileName].Add(contact);

                string res = "Document signers: \n";
                for (var i = 0; i < contactsdict[tgid][documentNameDictionary[tgid].FileName].Count; i++)
                {
                    Contact c = contactsdict[tgid][documentNameDictionary[tgid].FileName][i];
                    res = res + c.FirstName + " " + c.LastName + "\n";
                }
                
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, res, false, false, 0, keyboard);
            
            } else {
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Do not understand, sorry", false, false, 0, null );
            }
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
        
        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            // Keyboard with one button for sender sign
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(
                new Telegram.Bot.Types.InlineKeyboardButton[][]
                {
                    // First row
                    new [] {
                        // First column
                        new Telegram.Bot.Types.InlineKeyboardButton("sign document", "signcallback"),
                    },
                }
            );
            
            long tgid = callbackQueryEventArgs.CallbackQuery.From.Id;
            Document document = null;
            if (documentNameDictionary.ContainsKey(tgid))
            {
                document = documentNameDictionary[tgid];
            }
            
            
            if(callbackQueryEventArgs.CallbackQuery.Data == "sendtosigncallback") {
                //await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id, "You hav choosen " + callbackQueryEventArgs.CallbackQuery.Data, true);
                
                if (document != null) {
                    // Send to the magic blockchain endpoint (CAN BE SIGNED AND ALREADY SIGNED)
                    //String contractAddress, String docHash,
                    //String url, String senderAdress, String recipientAdress
                    var md5 = MD5.Create();
                    apiBlockChain.inizialWeb3();
                    var s =apiBlockChain.unlockAccount("0x3effa2d36eb2e09772f4195b89c6d11c322d626b", "123");
                    Console.WriteLine("account is lock: "+s);
                    var l = apiBlockChain.createDeal("0x6bff537405237294a5e786fda1fa8ea315e17b58","text1","texturl","0x3e165d74b72bc6848329ff8ddf678ac19ec1a139","0x3e165d74b72bc6848329ff8ddf678ac19ec1a139");
                    Console.WriteLine(l);
                    var adressDealContract=apiBlockChain.waitAddressAccount(l);
                    Console.WriteLine("adressContract"+adressDealContract);
                    await Bot.SendTextMessageAsync(tgid, "Document is sucessfully signed and send to: ", false, false, 0,null);
                    for (var i = 0; i < contactsdict[tgid][document.FileName].Count; i++)
                    {
                        Contact c = contactsdict[tgid][documentNameDictionary[tgid].FileName][i];
                        try
                        {
                           await Bot.SendTextMessageAsync(c.UserId, "Hey, you have new document for sign from: " + callbackQueryEventArgs.CallbackQuery.From.FirstName + " " + callbackQueryEventArgs.CallbackQuery.From.LastName, false, false, 0, null);
                           await Bot.SendDocumentAsync(c.UserId, document.FileId, "", false, 0, keyboard);
                           await Bot.SendTextMessageAsync(tgid, c.FirstName + c.LastName, false, false, 0,null);
                           Console.WriteLine("Send document to" + c.FirstName + " " + c.LastName);
                        }
                        catch (Exception e)
                        {
                            await Bot.SendTextMessageAsync(tgid, "We couldn't send document to " + c.FirstName + c.LastName, false, false, 0,null);
                            Console.WriteLine("Couldn't send document to" + c.FirstName + " " + c.LastName);
                            return;
                        }
                    }
                    await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id);
                    //Clean data and say success this to user
                    statedict[tgid] = "init";
                    documentNameDictionary = new Dictionary<long, Document>();
                    contactsdict = new Dictionary<long, Dictionary<string, List<Contact>>>();
                }
                else {
                    await Bot.SendTextMessageAsync(tgid, "Document is already signed", false, false, 0, null);
                }
            } else if (callbackQueryEventArgs.CallbackQuery.Data == "signcallback"){
                // Send request the magic blockchain endpoint
                await Bot.SendTextMessageAsync(tgid, "Document is sucessfully signed", false, false, 0, null );
                await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id);
            }
        }
    }
}