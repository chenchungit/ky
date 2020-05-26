using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Office.Interop.Excel;
using Excel = Microsoft.Office.Interop.Excel;
using Server.Tools;

namespace GameServer.cc.Skill
{
    public class SkillManager
    {
        public const string SkillPathName = "Config/skill/Skill.xml";
        public const string SkillRootName = "Skills";
        public const string SkillPathExcel = "Config/skill/Skill.xlsx";

        public Dictionary<int, SkillObject> SystemSkillList = new Dictionary<int, SkillObject>();
        public void InitSkill()
        {
            //InitExcel();
           // return;
            XElement szxXml = null;
            try
            {
                string fullPathFileName = Logic.Global.GameResPath(SkillPathName);
                szxXml = XElement.Load(fullPathFileName);
                if (null == szxXml)
                {
                    throw new Exception(string.Format("加载技能xml配置文件:{0}, 失败。没有找到相关XML配置文件!", fullPathFileName));
                }
                SystemXmlItem systemXmlItem = new SystemXmlItem();
                IEnumerable<XElement> nodes = szxXml.Elements(SkillRootName).Elements();
                foreach (var node in nodes)
                {
                    systemXmlItem.XMLNode = node;

                    SkillObject skillObject = new SkillObject()
                    {
                        SkillID = systemXmlItem.GetIntValue("SkillID"),
                        SkillName = systemXmlItem.GetStringValue("Name"),
                        Remark = systemXmlItem.GetStringValue("Remark"),
                        NeedMP = systemXmlItem.GetIntValue("NeedMP"),
                        NeedHP = systemXmlItem.GetIntValue("NeedHP"),
                        SkillType = systemXmlItem.GetIntValue("SkillType"),
                        Distance = systemXmlItem.GetIntValue("Distance"),
                        CDTime = systemXmlItem.GetIntValue("Cooling"),
                        HarmType = systemXmlItem.GetIntValue("HarmType"),
                        Occupation = systemXmlItem.GetIntValue("NeedJob"),
                        HardHarm = systemXmlItem.GetIntValue("HardHarm"),
                        NeedLV = systemXmlItem.GetIntValue("NeedLV"),
                        SkillLevel = systemXmlItem.GetIntValue("SkillLevel"),
                        Ballistic = systemXmlItem.GetIntValue("Ballistic"),
                        NeedSkill = null,
                        BecomeNeedSkill = null,
                        NextSkill = null,
                        BecomeNextSkill = null,
                        BuffList = null,
                        EffectList = null,
                        RangeType = null,
                        NeedWeapons = null,
                        SkillHarmList = null,
                        TargetType = systemXmlItem.GetIntValue("TargetType")
                    };
                    //务器类型
                    skillObject.NeedWeapons = new List<int>();
                    string[] szNeedWeapons = systemXmlItem.GetStringValue("NeedWeapons").Split('#');
                    foreach (var s in szNeedWeapons)
                    {
                        skillObject.NeedWeapons.Add(Convert.ToInt32(s));
                    }
                    //前置技能
                    skillObject.NeedSkill = new List<int>();
                    string[] szNeedSkill = systemXmlItem.GetStringValue("NeedSkill").Split('#');
                    foreach (var s in szNeedSkill)
                    {
                        skillObject.NeedSkill.Add(Convert.ToInt32(s));
                    }
                    //开化后前置技能
                    skillObject.BecomeNeedSkill = new List<int>();
                    string[] szBecomeNeedSkill = systemXmlItem.GetStringValue("BecomeNeedSkill").Split('#');
                    foreach (var s in szBecomeNeedSkill)
                    {
                        skillObject.BecomeNeedSkill.Add(Convert.ToInt32(s));
                    }
                    //后置技能
                    skillObject.NextSkill = new List<int>();
                    string[] szNextSkill = systemXmlItem.GetStringValue("NextSkill").Split('#');
                    foreach (var s in szNextSkill)
                    {
                        skillObject.NextSkill.Add(Convert.ToInt32(s));
                    }
                    //开花后得后置技能
                    skillObject.BecomeNextSkill = new List<int>();
                    string[] szBecomeNextSkill = systemXmlItem.GetStringValue("BecomeNextSkill").Split('#');
                    foreach (var s in szBecomeNextSkill)
                    {
                        skillObject.BecomeNextSkill.Add(Convert.ToInt32(s));
                    }
                    //影响范围
                    skillObject.RangeType = new List<int>();
                    string[] szRangeType = systemXmlItem.GetStringValue("RangeType").Split('#');
                    foreach (var s in szRangeType)
                    {
                        skillObject.RangeType.Add(Convert.ToInt32(s));
                    }
                    //技能伤害
                    skillObject.SkillHarmList = new List<int>();
                    string[] szSkillHarm = systemXmlItem.GetStringValue("SkillHarm").Split('#');
                    foreach (var s in szSkillHarm)
                    {
                        skillObject.SkillHarmList.Add(Convert.ToInt32(s));
                    }
                    //释放目标
                    //skillObject.TargetTypeList = new List<int>();
                    //string[] szTargetType = systemXmlItem.GetStringValue("TargetType").Split('#');
                    //foreach (var s in szTargetType)
                    //{
                    //    skillObject.TargetTypeList.Add(Convert.ToInt32(s));
                    //}
                    //释放类型
                    skillObject.ReleaseTypeList = new List<int>();
                    string[] szReleaseType = systemXmlItem.GetStringValue("ReleaseType").Split('#');
                    foreach (var s in szSkillHarm)
                    {
                        skillObject.ReleaseTypeList.Add(Convert.ToInt32(s));
                    }

                    SystemSkillList.Add(skillObject.SkillID, skillObject);
                    //SysConOut.WriteLine(string.Format("加载技能配置文件:{0},", skillObject.SkillID));
                }
            }
            catch (Exception ex)
            {
                SysConOut.WriteLine(string.Format("加载技能Excel配置文件:{0}, 失败。没有找到相关XML配置文件[{1}]!", SkillPathName, ex.Message));

               // throw new Exception(string.Format("加载技能xxml配置文件:{0}, 失败。没有找到相关XML配置文件!", SkillPathName));
            }
            SysConOut.WriteLine(string.Format("加载技能配置文件:{0},", SystemSkillList.Count));

        }

        public void InitExcel()
        {
            string fullPathFileName = Logic.Global.GameResPath(SkillPathExcel);
            Excel.Application xlApp = new Excel.Application();
            xlApp.DisplayAlerts = false;
            xlApp.Visible = false;
            xlApp.ScreenUpdating = false;
            Excel.Workbook xlsWorkBook = xlApp.Workbooks.Open(fullPathFileName, System.Type.Missing, System.Type.Missing, System.Type.Missing,
                            System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing,
                            System.Type.Missing, System.Type.Missing);
            Excel.Worksheet sheet = xlsWorkBook.Worksheets[1];//工作薄从1开始，不是0
            int i = 1;
            int j = 1;
            try
            {
                
                int iRowCount = sheet.UsedRange.Rows.Count;
                int iColCount = sheet.UsedRange.Columns.Count;
                Excel.Range range;
            
                for (;i< iRowCount;i++)
                {
                    SkillObject skillObject = new SkillObject();
                    for (j = 1; j< iColCount;j++)
                    {
                        range = sheet.Cells[i, j];
                        switch(j)
                        {
                            case 1:
                                {
                                    skillObject.SkillID =Convert.ToInt32((range.Value2 == null) ? "" : range.Text.ToString());
                                    break;
                                }
                            case 2:
                                {
                                    skillObject.SkillName = (range.Value2 == null) ? "" : range.Text.ToString();
                                    break;
                                }
                            case 3:
                                {
                                    skillObject.Remark = (range.Value2 == null) ? "" : range.Text.ToString();
                                    break;
                                }
                            case 4:
                                {
                                    skillObject.Occupation = Convert.ToInt32((range.Value2 == null) ? "" : range.Text.ToString());
                                    break;
                                }
                            case 5:
                                {
                                    skillObject.NeedMP = Convert.ToInt32((range.Value2 == null) ? "" : range.Text.ToString());
                                    break;
                                }
                            case 6:
                                {
                                    skillObject.NeedHP = Convert.ToInt32((range.Value2 == null) ? "" : range.Text.ToString());
                                    break;
                                }
                            case 7:
                                {
                                    skillObject.NeedLV = Convert.ToInt32((range.Value2 == null) ? "" : range.Text.ToString());
                                    break;
                                }
                            
                          
                            case 13:
                                {
                                    skillObject.SkillType = Convert.ToInt32((range.Value2 == null) ? "" : range.Text.ToString());
                                    break;
                                }
                           
                            case 15:
                                {
                                    skillObject.HarmType = Convert.ToInt32((range.Value2 == null) ? "" : range.Text.ToString());
                                    break;
                                }
                          
                            case 17:
                                {
                                    skillObject.HardHarm = Convert.ToInt32((range.Value2 == null) ? "" : range.Text.ToString());
                                    break;
                                }
                            case 18:
                                {
                                    skillObject.EffectList = null;
                                    break;
                                }
                            case 19:
                                {
                                    skillObject.BuffList = null;
                                    break;
                                }
                            case 20:
                                {
                                    skillObject.Distance = Convert.ToInt32((range.Value2 == null) ? "" : range.Text.ToString());
                                    break;
                                }
                            case 21:
                                {
                                    skillObject.CDTime = Convert.ToInt32((range.Value2 == null) ? "" : range.Text.ToString());
                                    break;
                                }
                            case 16:
                                {
                                    skillObject.SkillHarmList = new List<int>();
                                    string[] szSkillHarm = ((range.Value2 == null) ? "" : range.Text.ToString()).Split(',');
                                    foreach (var s in szSkillHarm)
                                    {
                                        skillObject.SkillHarmList.Add(Convert.ToInt32(s));
                                    }
                                    break;
                                }
                            case 14:
                                {
                                    skillObject.RangeType = new List<int>();
                                    string[] szRangeType = ((range.Value2 == null) ? "" : range.Text.ToString()).Split(',');
                                    foreach (var s in szRangeType)
                                    {
                                        skillObject.RangeType.Add(Convert.ToInt32(s));
                                    }
                                    break;
                                }
                            case 8:
                                {
                                    skillObject.NeedWeapons = new List<int>();
                                    string[] szNeedWeapons = ((range.Value2 == null) ? "" : range.Text.ToString()).Split(',');
                                    foreach (var s in szNeedWeapons)
                                    {
                                        skillObject.NeedWeapons.Add(Convert.ToInt32(s));
                                    }
                                    break;
                                }
                            case 9:
                                {
                                    skillObject.NeedSkill = new List<int>();
                                    string[] szNeedSkill = ((range.Value2 == null) ? "" : range.Text.ToString()).Split(',');
                                    foreach (var s in szNeedSkill)
                                    {
                                        skillObject.NeedSkill.Add(Convert.ToInt32(s));
                                    }
                                    break;
                                }
                            case 10:
                                {
                                    skillObject.BecomeNeedSkill = new List<int>();
                                    string[] szBecomeNeedSkill = ((range.Value2 == null) ? "" : range.Text.ToString()).Split(',');
                                    foreach (var s in szBecomeNeedSkill)
                                    {
                                        skillObject.BecomeNeedSkill.Add(Convert.ToInt32(s));
                                    }

                                    break;
                                }
                            case 11:
                                {
                                    skillObject.NextSkill = new List<int>();
                                    string[] szNextSkill = ((range.Value2 == null) ? "" : range.Text.ToString()).Split(',');
                                    foreach (var s in szNextSkill)
                                    {
                                        skillObject.NextSkill.Add(Convert.ToInt32(s));
                                    }
                                    break;
                                }
                            case 12:
                                {
                                    skillObject.BecomeNextSkill = new List<int>();
                                    string[] szBecomeNextSkill = ((range.Value2 == null) ? "" : range.Text.ToString()).Split(',');
                                    foreach (var s in szBecomeNextSkill)
                                    {
                                        skillObject.BecomeNextSkill.Add(Convert.ToInt32(s));
                                    }
                                    break;
                                }

                        }
                   
                    }
                    SystemSkillList.Add(skillObject.SkillID, skillObject);
                }
              
            }
            catch (Exception ex)
            {
                SysConOut.WriteLine(string.Format("加载技能Excel配置文件:{0}, 失败。没有找到相关XML配置文件[{1}] i ={2}  j={3}!", SkillPathName, ex.Message,i,j));
                // throw new Exception(string.Format("加载技能Excel配置文件:{0}, 失败。没有找到相关XML配置文件!", SkillPathName));
            }
            finally
            {
                xlsWorkBook.Close(false, System.Type.Missing, System.Type.Missing);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(xlsWorkBook);
                xlsWorkBook = null;
                xlApp.Workbooks.Close();
                xlApp.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
                xlApp = null;
            }

        }
    }
}
