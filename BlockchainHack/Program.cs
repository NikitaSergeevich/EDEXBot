﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

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
            InlineQueryResult[] results = {
                new InlineQueryResultLocation
                {
                    Id = "1",
                    Latitude = 40.7058316f, // displayed result
                    Longitude = -74.2581888f,
                    Title = "New York",
                    InputMessageContent = new InputLocationMessageContent // message if result is selected
                    {
                        Latitude = 40.7058316f,
                        Longitude = -74.2581888f,
                    }
                },

                new InlineQueryResultLocation
                {
                    Id = "2",
                    Longitude = 52.507629f, // displayed result
                    Latitude = 13.1449577f,
                    Title = "Berlin",
                    InputMessageContent = new InputLocationMessageContent // message if result is selected
                    {
                        Longitude = 52.507629f,
                        Latitude = 13.1449577f
                    }
                }
            };
            

            await Bot.AnswerInlineQueryAsync(inlineQueryEventArgs.InlineQuery.Id, results, isPersonal: true, cacheTime: 0);
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
            
            if (messageEventArgs.Message.Type.Equals(MessageType.TextMessage) && 
                messageEventArgs.Message.Text.Equals("/start")){
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Please Select An Option", false, false, 0, rkm );
            } else if (messageEventArgs.Message.Type.Equals(MessageType.TextMessage) && 
                       messageEventArgs.Message.Text.Equals("Upload")){
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Now upload file you want to sign", false, false, 0, null);
            } else if (messageEventArgs.Message.Type.Equals(MessageType.DocumentMessage)){
                await Bot.SendTextMessageAsync(messageEventArgs.Message.Chat.Id, "Please select document signers by phone number from your contact list or by alias", false, false, 0, null );
            }
        }

        private static async void BotOnCallbackQueryReceived(object sender,
            CallbackQueryEventArgs callbackQueryEventArgs)
        {
            await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
                $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
        }
    }
}