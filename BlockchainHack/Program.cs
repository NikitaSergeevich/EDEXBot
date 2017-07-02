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
        static Dictionary<long, string> docnamedict = new Dictionary<long, string>();
        
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
            
            //Console.Title = me.Username;
            Console.WriteLine(me.Username);

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

            long tgid = messageEventArgs.Message.Chat.Id;
            string docname = null;
            Contact contact = null;
            if (messageEventArgs.Message.Type.Equals(MessageType.DocumentMessage)){
                docname = messageEventArgs.Message.Document.FileName;
            } else if (messageEventArgs.Message.Type.Equals(MessageType.ContactMessage))
            {
                contact = messageEventArgs.Message.Contact;
            }
            
            if (messageEventArgs.Message.Type.Equals(MessageType.TextMessage) && 
                messageEventArgs.Message.Text.Equals("/start")){
                Console.WriteLine("Init");
                statedict[tgid] = "init";
                docnamedict = new Dictionary<long, string>();
                contactsdict = new Dictionary<long, Dictionary<string, List<Contact>>>();
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Please Select An Option", false, false, 0, rkm );
            } else if (messageEventArgs.Message.Type.Equals(MessageType.TextMessage) && 
                       messageEventArgs.Message.Text.Equals("Upload")){
                Console.WriteLine("Want to upload");
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Now upload file you want to sign", false, false, 0, new ReplyKeyboardMarkup());
            } else if (messageEventArgs.Message.Type.Equals(MessageType.DocumentMessage) && 
                       statedict[tgid] == "init"){
                Console.WriteLine("Document received");
                
                //Update Data
                statedict[tgid] = "uploaded";
                docnamedict[tgid] = docname;
                
                //Send Reply
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Please select document signers by phone number from your contact list or by alias", false, false, 0, null );
                
            } else if (messageEventArgs.Message.Type.Equals(MessageType.ContactMessage) && 
                       statedict[tgid] == "uploaded")
            {
                statedict[messageEventArgs.Message.Chat.Id] = "set";
                string s = docnamedict[tgid];
                List<Contact> t = contactsdict[messageEventArgs.Message.Chat.Id][s];
                t.Add(contact);
            }
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
                $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
        }
    }
}