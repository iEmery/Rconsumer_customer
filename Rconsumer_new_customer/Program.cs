using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rconsumer_new_customer.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml.Linq;

namespace Rconsumer_new_customer
{
    class Program
    {
        class Message
        {
            public string firstname { get; set; }
            public string lastname { get; set; }
            public string email { get; set; }
            public string birthday { get; set; }
            public string address1 { get; set; }
            public string address2 { get; set; }
            public string postcode { get; set; }
            public string city { get; set; }
            public string phone { get; set; }
            public string phone_mobile { get; set; }
        }
        static void Main(string[] args)
        {
            //WAIT FOR NEW MESSAGES
            var factory = new ConnectionFactory();
            factory.UserName = "guest";
            factory.Password = "guest";
            factory.HostName = "10.3.51.37";

            var conn = factory.CreateConnection();
            var channel = conn.CreateModel();

            channel.ExchangeDeclare(
                exchange: "new_customer_exchange",
                type: ExchangeType.Direct);
            channel.QueueDeclare(
                queue: "new_customer_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            channel.QueueBind(
                queue: "new_customer_queue",
                exchange: "new_customer_exchange",
                routingKey: "new_customer_queue");

            var suitecrm_consumer = new EventingBasicConsumer(channel);
            suitecrm_consumer.Received += async (IModel, ea) =>
            {
                //PULL DATA FROM MESSAGE
                var bodyString = Encoding.UTF8.GetString(ea.Body);
                List<Message> message_list = JsonConvert.DeserializeObject<List<Message>>(bodyString.ToString());
                await System.Threading.Tasks.Task.Run(() => DoSomeWork(message_list));
                
                //ACK
                channel.BasicAck(ea.DeliveryTag, false);
            };

            channel.BasicConsume(queue: "new_customer_queue", noAck: false, consumer: suitecrm_consumer);
        }

        private static void DoSomeWork(List<Message> message_list)
        {
            string sugarCrmUrl = "http://10.3.51.37:32400/service/v4_1/soap.php";
            string sugarCrmUsername = "User";
            string sugarCrmPassword = "6a4dc9133d5f3b6d9fff778aff361961";
            string sugarCrmModulename = "Accounts";
            


            //GET SUITECRM USERS
            ServiceReference1.sugarsoapPortTypeClient soap = new ServiceReference1.sugarsoapPortTypeClient();
            ServiceReference1.user_auth user = new ServiceReference1.user_auth();

            user.user_name = sugarCrmUsername;
            user.password = sugarCrmPassword;

            ServiceReference1.name_value[] loginList = new ServiceReference1.name_value[0];
            ServiceReference1.entry_value result_login = soap.login(user, "SOAP_RABBITMQ", loginList);
            
            string sessionId = result_login.id;

            ServiceReference1.get_entry_list_result_version2 result_suiteCrm = soap.get_entry_list(
                sessionId,
                "Accounts",
                "",
                "",
                0,
                new[] { "id", "name" },
                null,
                99999,
                0,
                false);



            //PROCES MESSAGES
            foreach (Message m in message_list)
            {
                int index = 0;
                foreach (entry_value var in result_suiteCrm.entry_list)
                {

                    string soapC_name = GetValueFromNameValueList("name", var.name_value_list);
                    string soapC_id = GetValueFromNameValueList("id", var.name_value_list);

                    //Console.WriteLine(name);
                    

                    //UPDATE
                    if (soapC_name.Contains(m.firstname) && soapC_name.Contains(m.lastname))
                    {

                        NameValueCollection fieldListCollection = new NameValueCollection();
                        //to update a record, you will nee to pass in a record id as commented below
                        fieldListCollection.Add("id", soapC_id);
                        //fieldListCollection.Add("name", "Test Account");

                        //this is just a trick to avoid having to manually specify index values for name_value[]
                        ServiceReference1.name_value[] fieldList = new ServiceReference1.name_value[fieldListCollection.Count];

                        int count = 0;
                        foreach (string name in fieldListCollection)
                        {
                            foreach (string value in fieldListCollection.GetValues(name))
                            {
                                ServiceReference1.name_value field = new ServiceReference1.name_value();
                                field.name = name; field.value = value;
                                fieldList[count] = field;
                            }
                            count++;
                        }

                        try
                        {
                            ServiceReference1.new_set_entry_result result_insert = soap.set_entry(sessionId, "Accounts", fieldList);
                            string RecordID = result_insert.id;

                            //show record id to user
                            Console.WriteLine(RecordID);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.Source);
                        }


                    }

                    //CREATE
                    else
                    {
                        soapC_name = m.firstname + m.lastname;
                        string soapC_billingA1 = m.address1;
                        string soapC_billingA2 = m.address2;
                        string soapC_billingCity = m.city;
                        string soapC_billingState = m.city;
                        string soapC_billingPostalcode = m.postcode;
                        string soapC_billingCountry = m.city;
                        string soapC_billingPhoneM = m.phone_mobile;
                        string soapC_billingPhone = m.phone;
                        string soapC_billingEmail = m.email;
                        

                        NameValueCollection fieldListCollection = new NameValueCollection();
                        //fieldListCollection.Add("id", "Test Account");
                        fieldListCollection.Add("name", soapC_name);
                        fieldListCollection.Add("billing_address_street", soapC_billingA1);
                        fieldListCollection.Add("billing_address_street_2", soapC_billingA2);
                        fieldListCollection.Add("billing_address_city", soapC_billingCity);
                        fieldListCollection.Add("billing_address_state", soapC_billingState);
                        fieldListCollection.Add("billing_address_postalcode", soapC_billingPostalcode);
                        fieldListCollection.Add("billing_address_country", soapC_billingCountry);
                        fieldListCollection.Add("phone_office", soapC_billingPhoneM);
                        fieldListCollection.Add("phone_fax", soapC_billingPhone);
                        fieldListCollection.Add("email1", soapC_billingEmail);

                        ServiceReference1.name_value[] fieldList = new ServiceReference1.name_value[fieldListCollection.Count];
                        int count = 0;
                        foreach (string name in fieldListCollection)
                        {
                            foreach (string value in fieldListCollection.GetValues(name))
                            {
                                ServiceReference1.name_value field = new ServiceReference1.name_value();
                                field.name = name; field.value = value;
                                fieldList[count] = field;
                            }
                            count++;
                        }

                        try
                        {
                            ServiceReference1.new_set_entry_result result_insert = soap.set_entry(sessionId, "Accounts", fieldList);
                            string RecordID = result_insert.id;

                            //show record id to user
                            Console.WriteLine(RecordID);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.Source);
                        }
                    }
                    index++;

                    

                }

            }



        }
        private static string GetValueFromNameValueList(string key, IEnumerable<name_value> nameValues)
        {
            return nameValues.Where(nv => nv.name == key).ToArray()[0].value;
        }
    }
}
