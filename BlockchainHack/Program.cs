﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
 using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        
        private static readonly TelegramBotClient Bot = new TelegramBotClient("408098530:AAGKKN-C7R2wbLcIqE0T17v7RZeNO25I8fQ");

        static Dictionary<long, Dictionary<String, List<Telegram.Bot.Types.Contact>>> contactsdict = new Dictionary<long, Dictionary<String, List<Telegram.Bot.Types.Contact>>>();
        static Dictionary<long, string> statedict = new Dictionary<long, string>();
        static Dictionary<long, Document> docnamedict = new Dictionary<long, Document>();
        
        public static void Main(string[] args)
        {
            /*var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();*/
            
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
                docnamedict = new Dictionary<long, Document>();
                contactsdict = new Dictionary<long, Dictionary<string, List<Contact>>>();
                //Set empty / clear contact list for client with tgid
                contactsdict[tgid] = new Dictionary<string, List<Contact>>();
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Now upload file you want to sign", false, false, 0, null);
            } else if (messageEventArgs.Message.Type.Equals(MessageType.DocumentMessage) && 
                       statedict[tgid] == "init"){
                Console.WriteLine("Document received from " + tgid);
                
                //Update Data
                statedict[tgid] = "uploaded";
                docnamedict[tgid] = messageEventArgs.Message.Document;
                
                //
                Dictionary<string, List<Contact>> dict = new Dictionary<string, List<Contact>>();
                dict[docname] = new List<Contact>();
                contactsdict[tgid] = dict;
                
                //Send Reply
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Document for sign: " + docname + ". Please select document signers by phone number from your contact list", false, false, 0, null );
                
            } else if (messageEventArgs.Message.Type.Equals(MessageType.ContactMessage) && 
                       statedict[tgid] == "uploaded" || statedict[tgid] == "set")
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
                contactsdict[tgid][docnamedict[tgid].FileName].Add(contact);

                string res = "Document signers: \n";
                for (var i = 0; i < contactsdict[tgid][docnamedict[tgid].FileName].Count; i++)
                {
                    Contact c = contactsdict[tgid][docnamedict[tgid].FileName][i];
                    res = res + c.FirstName + " " + c.LastName + "\n";
                }
                
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, res, false, false, 0, keyboard);
            
            } else {
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Do not understand, sorry", false, false, 0, null );
            }
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
            Document d = null;
            if (docnamedict[tgid] != null)
            {
                d = docnamedict[tgid];
            }
            
            
            if(callbackQueryEventArgs.CallbackQuery.Data == "sendtosigncallback") {
                //await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id, "You hav choosen " + callbackQueryEventArgs.CallbackQuery.Data, true);
                
                // Send to the magic blockchain endpoint (CAN BE SIGNED AND ALREADY SIGNED)
                
                for (var i = 0; i < contactsdict[tgid][d.FileName].Count; i++)
                {
                    Contact c = contactsdict[tgid][docnamedict[tgid].FileName][i];
                    await Bot.SendTextMessageAsync(c.UserId, "Hey, you have new document for sign from" + c.FirstName + " " + c.LastName, false, false, 0, null);
                    Console.WriteLine("Send document to" + c.FirstName + " " + c.LastName);
                    await Bot.SendDocumentAsync(c.UserId, d.FileId, "", false, 0, keyboard);
                }
                
                if (d != null) {
                    //Clean data and say success this to user
                    statedict[tgid] = "init";
                    docnamedict = new Dictionary<long, Document>();
                    contactsdict = new Dictionary<long, Dictionary<string, List<Contact>>>();
                    await Bot.SendTextMessageAsync(tgid, "Document is sucessfully signed and send", false, false, 0,null);
                }
                else {
                    await Bot.SendTextMessageAsync(tgid, "Document is already signed", false, false, 0, null);
                }
            } else if (callbackQueryEventArgs.CallbackQuery.Data == "signcallback"){
                // Send request the magic blockchain endpoint
                await Bot.SendTextMessageAsync(tgid, "Document is sucessfully signed", false, false, 0, null );
            }
        }
    }
}