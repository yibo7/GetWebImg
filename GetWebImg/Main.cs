using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EbSite.ApiEntity;
using XS.Core;
using XS.Core.WebApiUtils;

namespace GetWebImg
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;

            wbB.ScrollBarsEnabled = false;
            wbB.ScriptErrorsSuppressed = true; //设置为True,可禁止弹出脚本错误对话框
            wbB.Width = m_BrowserWidth;
            wbB.Height = m_BrowserHeight;
            //wbB.Navigate("http://www.com");
            bll = new ApiBll(sAPI, key);
        }

        private EbSite.ApiEntity.Content model;
        string key = "3z0Pt7mTIieshFoPvpzGws2YoBZQ8XXASuDyILPmH1Mv8cUsfE+Phb2WNmBHbt4SDwenN9HGOq2BfWVxVWYFKcG9w3uwn2JuZjwkD1ryAyAr3ky4jrSjpqvi7gjl90/ht45LZE2rmBu7e6diU9HeT6szs0wlQn2RiwpuBFswL+2z1rdhbcu7tpVYw/CtM9Kah7tuHC5ZT+kUfiXX28vu3g==";
        private string sAPI = "http://anli.ebsite.net/api";
        private ApiBll bll;
        private bool IsStop = true;
        private void Goto()
        {

            ContentQueryRezult rz = GetModel();

            if (!Equals(rz,null)&&rz.Count > 0)
            {
                IsStop = false;
                   model =  rz.Data[0];

                string sUrl = model.Annex3;
                stInfo.Text = string.Format("正在载入{0}", sUrl);
                wbB.Navigate(sUrl);
                 
            }
            else
            {
                IsStop = true;
            }

        }

        private ContentQueryRezult GetModel()
        {
            EbSite.ApiEntity.QueryModel mdQueryModel = new QueryModel();
            mdQueryModel.SiteId = 1;
            mdQueryModel.Fields = "";
            mdQueryModel.PageSize = 1;
            mdQueryModel.PageIndex = 1;
            mdQueryModel.OrderBy = "id ASC";


            List<ContentQuery> cqs = new List<ContentQuery>();
            ContentQuery cq = new ContentQuery();
            cq.ColumName = "ClassID";
            cq.ColumValue = "1";//注意，如果是区间查询，这里要填写两个值，用逗号分开
            cq.QueryType = ContentQueryType.精确;//默认为精确查询
            cq.LinkType = ContentQueryLinkType.AND;//与下一个字段的链接方式，如果是最后一个字段，可不用填写
            cqs.Add(cq);

            cq = new ContentQuery();
            cq.ColumName = "Annex3";
            cq.ColumValue = "''";
            cq.QueryType = ContentQueryType.不等于;
            cqs.Add(cq);

            cq = new ContentQuery();
            cq.ColumName = "Annex7";
            cq.QueryType = ContentQueryType.是否为空IsNull;
            cqs.Add(cq);




            mdQueryModel.Wheres = cqs;//你可以注掉此条件，将查找所有数据

            ContentQueryRezult rz = bll.PostModel<ContentQueryRezult>("queryc/loadlist", mdQueryModel);

            return rz;
        }

        private int m_BrowserWidth = 1440;
        private int m_BrowserHeight = 1440;
        private int imgWidth = 1440;
        private int imgHeight = 1440;

        private void wbB_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            
            stInfo.Text = string.Format("{0}载入完成", model.Annex3);

            Thread st = new Thread(() =>
            {
                if (!string.IsNullOrEmpty(model.Annex1)) //url的md5
                {

                    WebBrowser m_WebBrowser = (WebBrowser)sender;
                    Thread.Sleep(3000);
                    Bitmap m_Bitmap = new Bitmap(m_WebBrowser.Bounds.Width, m_WebBrowser.Bounds.Height);
                    m_WebBrowser.BringToFront();
                    m_WebBrowser.DrawToBitmap(m_Bitmap, m_WebBrowser.Bounds);
                    m_Bitmap = (Bitmap)m_Bitmap.GetThumbnailImage(imgWidth, imgHeight, null, IntPtr.Zero);
                    string sUrl = wbB.Url.ToString();
                    string sMd5 = model.Annex1;// XS.Core.XsUtils.MD5(sUrl);
                    string sVPath = string.Format("webimg/{0}.png", sMd5);
                    string sFileName = string.Format("{0}{1}{2}.png", Application.StartupPath, "\\webimg\\", sMd5);
                    //System.Drawing.Image image = new System.Drawing.Bitmap(m_Bitmap);
                    //image.Save(sFileName);
                    model.Annex7 = sVPath;
                    try
                    {
                        m_Bitmap.Save(sFileName);

                        long KSize = (new System.IO.FileInfo(sFileName)).Length;
                        float testFloat = 0;

                        if (float.TryParse(Math.Round(KSize / (float)1024, 2).ToString(), out testFloat))
                        {
                            model.Annex19 = testFloat;
                        }


                        stInfo.Text = string.Format("{0}快照保存成功", sUrl);






                    }
                    catch (Exception exception)
                    {
                        model.Annex19 = 0;

                    }

                    ApiMessage<int> rzUpdate = bll.PostModel<ApiMessage<int>>("content", model);

                    stInfo.Text = string.Format("{0}快照保存成功并提交更新", sUrl);
                    Thread.Sleep(2000);
                }
                
                Goto();


            });
            st.SetApartmentState(ApartmentState.STA);
            st.Start();



        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            timer1.Start();
            timer1_Tick(sender, e);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (IsStop)
            {
                ContentQueryRezult rz = GetModel();

                if (!Equals(rz, null) && rz.Count > 0)
                {
                    Goto();
                }
               
            }
           
        }

        private void Main_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)  //判断是否最小化
            {
                this.ShowInTaskbar = false;  //不显示在系统任务栏
                notifyIcon.Visible = true;  //托盘图标可见
                this.Hide();
            }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.ShowInTaskbar = true;  //显示在系统任务栏
            this.WindowState = FormWindowState.Normal;  //还原窗体
            notifyIcon.Visible = false;  //托盘图标隐藏

           
        }
    }
     
}
