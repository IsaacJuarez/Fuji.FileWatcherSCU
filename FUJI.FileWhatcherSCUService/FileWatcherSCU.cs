using Dicom.Network;
using FUJI.FileWhatcherSCUService.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FUJI.FileWhatcherSCUService
{
    partial class FileWatcherSCU : ServiceBase
    {
        private StringBuilder m_Sb;
        private System.IO.FileSystemWatcher m_Watcher;
        public static clsConfiguracion _conf;
        public static string AETitle = "";
        public static string vchPathRep = "";
        public static string path = "";
        public FileWatcherSCU()
        {
            Log.EscribeLog("Inicio del Servicio SCU");
            cargarServicio();
        }

        private void cargarServicio()
        {
            try
            {
                
                try
                {
                    path = ConfigurationManager.AppSettings["ConfigDirectory"] != null ? ConfigurationManager.AppSettings["ConfigDirectory"].ToString() : "";
                }
                catch (Exception ePath)
                {
                    path = "";
                    Log.EscribeLog("Error al obtener el path desde appSettings: " + ePath.Message);
                }
                if (File.Exists(path + "info.xml"))
                {
                    _conf = XMLConfigurator.getXMLfile();
                    AETitle = _conf.vchAETitle;
                    vchPathRep = _conf.vchPathLocal;
                }
                m_Watcher = new System.IO.FileSystemWatcher();

                m_Watcher.Filter = "*.dcm*";
                m_Watcher.Path = vchPathRep;
                m_Watcher.IncludeSubdirectories = true;
                m_Watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                     | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                //m_Watcher.Changed += new FileSystemEventHandler(OnChanged);
                m_Watcher.Created += new FileSystemEventHandler(OnChanged);
                //m_Watcher.Deleted += new FileSystemEventHandler(OnChanged);
                m_Watcher.Renamed += new RenamedEventHandler(OnChanged);
                m_Watcher.EnableRaisingEvents = true;
            }
            catch (Exception eCS)
            {
                Log.EscribeLog("Error al cargar el servicio: " + eCS.Message);
            }
        }

        private async void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                Log.EscribeLog("Se intenta envío el archivo " + Path.GetFileName(e.FullPath));
                m_Sb = new StringBuilder();
                m_Sb.Remove(0, m_Sb.Length);
                m_Sb.Append(e.FullPath);
                m_Sb.Append(" ");
                m_Sb.Append(e.ChangeType.ToString());
                m_Sb.Append("    ");
                m_Sb.Append(DateTime.Now.ToString());
                Log.EscribeLog(m_Sb.ToString());
                string valido = "";
                try
                {
                    valido = await sendFile(e.FullPath);
                }
                catch(Exception eSend)
                {
                    valido = "";
                    Log.EscribeLog("Error al enviar en Onchange: " + eSend.Message);
                }
            }
            catch(Exception eOC)
            {
                Log.EscribeLog("Existe un error en el evento Onchange: " + eOC.Message);
            }
        }

        private Task<string> sendFile(string fullpath)
        {
            return Task.Run(() =>
            {
                string respuesta = "";
                try
                {
                    try
                    {
                        var client = new DicomClient();
                        client.NegotiateAsyncOps();
                        client.AddRequest(new DicomCEchoRequest());
                        client.AddRequest(new DicomCStoreRequest(fullpath));
                        string _ser = _conf.vchIPServidor;
                        int port = _conf.intPuertoServer;
                        string _aetS = _conf.vchAETitle;
                        string _aetA = _conf.vchAETitleServer;

                        if (_ser != "" && port > 0 && _aetA != "" && _aetS != "")
                        {
                            client.Send(_ser, port, false, _aetS, _aetA);
                            respuesta = "1";
                        }
                        else
                        {
                            Log.EscribeLog("Los parámetros para el envío no estan completos, favor de verificar: ");
                            Log.EscribeLog("IP Servidor destino: " + _ser);
                            Log.EscribeLog("Puerto Servidor destino: " + port.ToString());
                            Log.EscribeLog("AETitle Local: " + _aetS);
                            Log.EscribeLog("AETitle Server: " + _aetA);
                            respuesta = "0";
                        }
                    }
                    catch (Exception eENVIar)
                    {
                        respuesta = "0";
                        Log.EscribeLog("Existe un error al enviar el archivo:" + eENVIar.Message);
                        Console.WriteLine("Error al enviar el Estudio:" + eENVIar.Message);
                    }
                }
                catch (Exception esf)
                {
                    respuesta = "0";
                    Log.EscribeLog("Error: " + esf.Message);
                }
                moverFile(fullpath, respuesta);
                return respuesta;
            });
        }

        //private bool sendFile(string fullpath)
        //{
        //    bool enviado = false;
        //    try
        //    {
        //        try
        //        {
        //            var client = new DicomClient();
        //            client.NegotiateAsyncOps();
        //            client.AddRequest(new DicomCEchoRequest());
        //            client.AddRequest(new DicomCStoreRequest(fullpath));
        //            string _ser =_conf.vchIPServidor;
        //            int port = _conf.intPuertoServer;
        //            string _aetS = _conf.vchAETitle;
        //            string _aetA = _conf.vchAETitleServer;

        //            if(_ser !="" && port>0 && _aetA != "" && _aetS != "")
        //            {
        //                client.Send(_ser, port, false, _aetS, _aetA);
        //                enviado = true;
        //            }
        //            else
        //            {
        //                Log.EscribeLog("Los parámetros para el envío no estan completos, favor de verificar: ");
        //                Log.EscribeLog("IP Servidor destino: " + _ser);
        //                Log.EscribeLog("Puerto Servidor destino: " + port.ToString());
        //                Log.EscribeLog("AETitle Local: " + _aetS);
        //                Log.EscribeLog("AETitle Server: " + _aetA);
        //                enviado = false;
        //            }
        //        }
        //        catch (Exception eENVIar)
        //        {
        //            enviado = false;
        //            Log.EscribeLog("Existe un error al enviar el archivo:" + eENVIar.Message);
        //            Console.WriteLine("Error al enviar el Estudio:" + eENVIar.Message);
        //        }
        //    }
        //    catch (Exception esf)
        //    {
        //        enviado = false;
        //        Log.EscribeLog("Error: " + esf.Message);
        //    }
        //    return enviado;
        //}

        private void moverFile(string fullpath, string Correcto)
        {
            try
            {
                if (!Directory.Exists(path + @"Exito\"))
                    Directory.CreateDirectory(path + @"Exito\");
                if (!Directory.Exists(path + @"Error\"))
                    Directory.CreateDirectory(path + @"Error\");

                if (Correcto == "" || Correcto == "1")
                {
                    Log.EscribeLog("Archivo " + Path.GetFileName(fullpath) + " correcto");
                    File.Move(fullpath, path + @"Exito\" + Path.GetFileName(fullpath));
                }
                else
                {
                    Log.EscribeLog("Archivo " + Path.GetFileName(fullpath) + " con errores.");
                    File.Move(fullpath, path + @"Error\" + Path.GetFileName(fullpath));
                }
            }
            catch(Exception eMF)
            {
                Log.EscribeLog("Existe un error al mover el archivo: " + eMF.Message);
            }
        }

        protected override void OnStart(string[] args)
        {
            // TODO: agregar código aquí para iniciar el servicio.
        }

        protected override void OnStop()
        {
            // TODO: agregar código aquí para realizar cualquier anulación necesaria para detener el servicio.
        }
    }
}
