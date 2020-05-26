using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Threading;
using System.Xml.Linq;
using GameServer.Interface;
using Server.Data;
using Server.Tools;

namespace GameServer.Logic
{
    /// <summary>
    /// 技能名称定义
    /// </summary>
    public enum MagicActionIDs
    {
        FOREVER_ADDHIT = 0, //永久增加命中率	1级增加绝对值	2级增加绝对值	3级增加绝对值				
        RANDOM_ADDATTACK1, //概率增加攻击力(绝对值)【强攻剑术】	1级触发的概率	1级增加的攻击力	2级触发的概率	2级增加的攻击力	3级触发的概率	3级增加的攻击力	
        RANDOM_ADDATTACK2, //增加攻击力(区间计算)(百分比）【战圣烈焰】	1级概率最小值	1级概率最大值	2级概率最小值	2级概率最大值	3级概率最小值	3级概率最大值	
        ATTACK_STRAIGHT, //攻击前面两格，针对隔位攻击无视闪避、无视防御发挥攻击力X%	1级百分比	2级百分比	3级百分比				
        ATTACK_FRONT, //物理伤害，攻击前、左、右两格，针对左、右攻击无视闪避、无视防御发挥正常攻击力40%的X%。	1级百分比	2级百分比	3级百分比				
        PUSH_STRAIGHT, //将释放者前方等级低于自己的敌对目标推开两格，附加伤害40点	1级附加伤害	2级附加伤害	3级附加伤害				
        PUSH_CIRCLE, //将周围3*3范围内（不包含中心）等级低于释放者的敌对目标向外推开一格	1级附加伤害	2级附加伤害	3级附加伤害				
        MAGIC_ATTACK, //魔法伤害，附加伤害X1-X2点	1级附加最小值	1级附加最大值	2级附加最小值	2级附加最大值	3级附加最小值	3级附加最大值	
        DS_ATTACK, //道术伤害，附加伤害X1-X2点	1级附加最小值	1级附加最大值	2级附加最小值	2级附加最大值	3级附加最小值	3级附加最大值	
        RANDOM_MOVE, //本地图随机移动，有几率回城，有几率无效，等级越高无效几率越低	1级无效概率	2级无效概率	3级无效概率				
        FIRE_WALL, //目标3*3范围内魔法伤害，持续X秒，造成X%攻击力伤害,间隔X秒	持续时间(秒)	间隔时间(秒)	1级攻击力百分比	2级攻击力百分比	3级攻击力百分比		
        FIRE_CIRCLE, //对以释放者为中心5*5范围对目标造成魔法伤害，造成X%攻击力伤害（对玩家无效）	1级攻击力百分比	2级攻击力百分比	3级攻击力百分比				
        NEW_MAGIC_SUBINJURE, //给释放者增加一个魔法护盾，吸收X比例伤害，持续X分钟	持续时间(秒)	1级吸收伤害百分比	2级吸收伤害百分比	3级吸收伤害百分比			
        DS_ADDLIFE, //恢复单体目标生命值（固定）（持续恢复,类似喝药)	1级加固定值	2级加固定值	3级加固定值				
        DS_CALL_GUARD, //召唤卫士	召唤的卫士ID						
        DS_HIDE_ROLE, //隐身	持续时间(秒)						
        TIME_DS_ADD_DEFENSE, //增加物理防御力(持续)	持续时间(秒)	1级增加绝对值最小	1级增加绝对值最大	2级增加绝对值最小	2级增加绝对值最大	3级增加绝对值最小	3级增加绝对值最大
        TIME_DS_ADD_MDEFENSE, //增加魔法防御力(持续)	持续时间(秒)	1级增加绝对值最小	1级增加绝对值最大	2级增加绝对值最小	2级增加绝对值最大	3级增加绝对值最小	3级增加绝对值最大
        TIME_DS_SUB_DEFENSE, //减少目标防御X1-X1点	持续时间(秒)	1级减防最小值	1级减防最大值	2级减防最小值	2级减防最大值	3级减防最小值	3级减防最大值
        TIME_DS_INJURE, //持续伤害	持续时间(秒)	间隔时间(秒)	1级攻击力的百分比	2级攻击力的百分比	3级攻击力的百分比	
        PHY_ATTACK,	//物理伤害，附加伤害X1-X2点	1级附加最小值	1级附加最大值	2级附加最小值	2级附加最大值	3级附加最小值	3级附加最大值

        INSTANT_ATTACK,	//直接物理伤害	物理攻击的百分比		
        INSTANT_MAGIC,	//直接魔法伤害	魔法攻击的百分比		
        INSTANT_ATTACK1, //直接物理伤害 + 多少值 要增加的物理伤害值 *
        INSTANT_MAGIC1, //直接魔法伤害 + 多少值	要增加的魔法伤害值 *
        INSTANT_ATTACK2LIFE, //直接物理伤害 + 多少值, 并将多少百分比的伤害转换为自己的血量	要增加的物理伤害值	转换伤害的百分比 *
        INSTANT_MAGIC2LIFE, //直接魔法伤害 + 多少值, 并将多少百分比的伤害转换为自己的血量	要增加的魔法伤害值	转换伤害的百分比 *
        TIME_ATTACK,	//持续物理伤害	物理伤害的百分比	持续多长时间	总共几次
        TIME_MAGIC,	//持续魔法伤害	魔法伤害的百分比	持续多长时间	总共几次
        FOREVER_ADDDEFENSE,	//永久增加物理防御力	增加多少值的防御力		
        FOREVER_ADDATTACK,	//永久增加物理攻击力	增加多少值的攻击力		
        FOREVER_ADDMAGICDEFENSE,	//永久增加魔法防御力	增加多少值的防御力		
        FOREVER_ADDMAGICATTACK,	//永久增加魔法攻击力	增加多少值的攻击力		
        TIME_ADDDEFENSE,	//持续增加物理防御力	增加百分比	持续多长时间	
        TIME_SUBDEFENSE,	//持续降低物理防御力	降低百分比	持续多长时间	
        TIME_ADDATTACK,	//持续增加物理攻击力	增加百分比	持续多长时间	
        TIME_SUBATTACK,	//持续降低物理攻击力	降低百分比	持续多长时间	
        TIME_ADDMAGICDEFENSE,	//持续增加魔法防御力	增加百分比	持续多长时间	
        TIME_SUBMAGICDEFENSE,	//持续降低魔法防御力	降低百分比	持续多长时间	
        TIME_ADDMAGIC,	//持续增加魔法攻击力	增加百分比	持续多长时间	
        TIME_SUBMAGIC,	//持续降低魔法攻击力	降低百分比	持续多长时间	
        INSTANT_ADDLIFE1,	//直接加血	加的血量		
        INSTANT_ADDMAGIC1,	//直接加魔	加的魔量		
        INSTANT_ADDLIFE2,	//直接加血	加的血量(自身总血量百分比)		
        INSTANT_ADDMAGIC2,	//直接加魔,同时解除技能地煞归宗、瞬移心法的冷却时间	加的魔量(自身总魔量百分比)
        INSTANT_ADDLIFE3,	//直接加血	消耗魔法值基础上增加的绝对数值 *
        INSTANT_ADDLIFE4,	//直接加血	消耗魔法值基础上乘以的百分比系数 *
        INSTANT_COOLDOWN,	//解除其他技能的冷却时间	节能ID		
        TIME_SUBLIFE,	//持续伤血	每次伤害多少点血	持续多长时间	总共几次
        TIME_ADDLIFE,	//继续加血	每次加多少点血	持续多长时间	总共几次
        TIME_SLOW,	//继续减速	减慢到原来速度的百分比	持续多长时间	
        TIME_ADDDODGE,	//继续增加闪避值	增加的百分比	持续多长时间	
        TIME_FREEZE,	//使目标冰冻无法移动	持续多长时间		
        TIME_INJUE2LIFE,	//将伤害转换为自己的生命	转换伤害的百分比	持续多长时间	
        INSTANT_BURSTATTACK,	//提高物理攻击力，符合条件暴击	提高的物理攻击力的百分比	当目标血量低于自身血量的的百分比	
        FOREVER_ADDDRUGEFFECT,	//提高药品使用效果	提高的百分比		
        INSTANT_REMOVESLOW,	//移除自身受到的速度限制效果			
        TIME_SUBINJUE,	//持续减少伤害	固定的伤害值	持续多长时间	
        TIME_ADDINJUE,	//持续增加伤害	固定的伤害值	持续多长时间
        TIME_SUBINJUE1,	//持续减少伤害(按照百分比)	减少的百分比系数	持续多长时间 *
        TIME_ADDINJUE1,	//持续增加伤害(按照百分比)	增加的百分比系数	持续多长时间 *
        TIME_DELAYATTACK,	//延迟物理攻击	物理攻击的百分比	延迟多少时间
        TIME_DELAYMAGIC,	//延迟魔法攻击	魔法攻击的百分比	延迟多少时间
        FOREVER_ADDDODGE,	//永久增加闪避值	增加的百分比
        TIME_INJUE2MAGIC,	//将伤害转换为自己的魔法消耗	转换伤害的百分比	持续多长时间
        FOREVER_ADDMAGICV,	//永久增加魔法值	增加数值	
        FOREVER_ADDMAGICRECOVER,	//永久增加魔法值恢复速度	增加百分比	
        FOREVER_ADDLIFE,	//永久增加生命值	增加绝对的数值 *
        INSTANT_MOVE,	//瞬移		
        INSTANT_STOP,	//施展技能后2秒内无法使用其他技能	技能id	持续多长时间
        TIME_ADDMAGIC1,	//持续加魔	加的魔量	持续多长时间	总共几次
        GOTO_MAP,	//回某个地图的固定的位置	地图编号		
        INSTANT_MAP_POS,	//随机传送到当前地图的某个位置	
        GOTO_LAST_MAP, //回上一个地图的最后位置
        ADD_HORSE,	//添加一个坐骑	坐骑的编号
        ADD_PET,	//添加一个宠物	宠物的编号
        ADD_HORSE_EXT, //添加坐骑的扩展属性	属性索引编号	添加的值
        ADD_PET_GRID, //为宠物的移动仓库添加扩展的格子	 扩展的格子个数
        ADD_SKILL,	//添加一个新的技能	技能ID 技能级别
        NEW_INSTANT_ATTACK, //直接物理伤害	原始物理攻击力要乘以的系数值	每增加一级，增加的物理攻击力值	
        NEW_INSTANT_MAGIC, //直接魔法伤害	原始魔法攻击力要乘以的系数值	每增加一级，增加的魔法攻击力值	
        NEW_FOREVER_ADDDEFENSE, //永久加物理防御	每增加一级，永久增加物理防御力值		
        NEW_FOREVER_ADDATTACK, //永久加物理攻击	每增加一级，永久增加的物理攻击力值		
        NEW_FOREVER_ADDMAGICDEFENSE, //永久加魔法防御	每增加一级，永久增加魔法防御力值		
        NEW_FOREVER_ADDMAGICATTACK, //永久加魔法攻击	每增加一级，永久增加魔法攻击力值		
        NEW_FOREVER_ADDHIT, //永久加命中	每增加一级，永久增加命中率		
        NEW_FOREVER_ADDDODGE, //永久加闪避	每增加一级，永久增加闪避值		
        NEW_FOREVER_ADDBURST, //永久加暴击	每增加一级，永久增加暴击值		
        NEW_FOREVER_ADDMAGICV, //永久加魔法值上限	每增加一级，永久增加魔法值		
        NEW_FOREVER_ADDLIFE, //永久加生命值上限	每增加一级，永久增加生命值		
        NEW_TIME_INJUE2MAGIC, //持续的用魔法抵消伤害	每增加一级，将伤害转换为自己的魔法消耗值	持续多长时间	
        NEW_TIME_ATTACK, //持续物理伤害	每增加一级，增加的物理伤害值	持续多长时间	总共几次
        NEW_TIME_MAGIC, //持续魔法伤害	每增加一级，增加的魔法伤害值	持续多长时间	总共几次
        NEW_INSTANT_ADDLIFE, //直接加血	每增加一级，加的血量值
        DB_ADD_DBL_EXP,	//添加打怪时双倍经验的buffer项	多长时间(单位:分钟)		
        DB_ADD_DBL_MONEY,	//添加打怪时双倍金币的buffer项	多长时间(单位:分钟)		
        DB_ADD_DBL_LINGLI,	//添加打怪时双倍灵力的buffer项	多长时间(单位:分钟)		
        DB_ADD_LIFERESERVE,	//生命储备	总共多少点的生命值储备	几秒钟增加一次	每秒添加多少
        DB_ADD_MAGICRESERVE,	//魔法储备	总共多少点的魔法值储备	几秒钟增加一次	每秒添加多少
        DB_ADD_LINGLIRESERVE,	//灵力储备	总共多少点的灵力值储备
        DB_ADD_TEMPATTACK,	//狂攻符咒	多长时间(单位:分钟)	增加的百分比(整数，例如百分之三十, 就写 30）	
        DB_ADD_TEMPDEFENSE,	//防御符咒	多长时间(单位:分钟)	增加的百分比(整数，例如百分之三十, 就写 30）	
        DB_ADD_UPLIEFLIMIT,	//增加生命上限	多长时间(单位:分钟)	增加的百分比(整数，例如百分之三十, 就写 30）	
        DB_ADD_UPMAGICLIMIT,	//增加魔法上限	多长时间(单位:分钟)	增加的百分比(整数，例如百分之三十, 就写 30）
        NEW_ADD_LINGLI,	//增加灵力	增加的灵力值
        NEW_ADD_MONEY,	//增加金币	增加的金币的数量
        NEW_ADD_EXP,	//增加经验	增加的经验的值
        NEW_ADD_YINLIANG,	//增加银两	增加的银两的值
        NEW_ADD_DAILYCXNUM,	//增加每日的冲穴次数	增加的每日冲穴次数的值
        GOTO_NEXTMAP,	//进一步下一层副本地图
        GET_AWARD,	//获取当前副本地图的奖励
        NEW_INSTANT_ADDLIFE2, //直接加血(魔法攻击量 + 增加的总血量)	每增加一级，加的血量值	乘以自身攻击力的系数
        NEW_INSTANT_ATTACK3, //直接物理伤害	原始物理攻击力要乘以的系数值(浮点数)	每增加一级，增加的物理攻击力系数值(浮点数)	
        NEW_INSTANT_MAGIC3, //直接魔法伤害	原始魔法攻击力要乘以的系数值(浮点数)	每增加一级，增加的魔法攻击力系数值(浮点数)	
        NEW_TIME_ATTACK3, //持续物理伤害	原始物理攻击力要乘以的系数值(浮点数)	每增加一级，增加的物理攻击力系数值(浮点数)	持续多长时间	总共几次
        NEW_TIME_MAGIC3, //持续魔法伤害	原始魔法攻击力要乘以的系数值(浮点数)	每增加一级，增加的魔法攻击力系数值(浮点数)	持续多长时间	总共几次
        NEW_INSTANT_ADDLIFE3, //直接加血	原始魔法攻击力要乘以的系数值(浮点数)	每增加一级，增加的魔法攻击力系数值(浮点数)	
        NEW_TIME_INJUE2MAGIC3, //持续的用魔法抵消伤害	原始将伤害转换为自己的魔法值的比例	每增加一级，将伤害转换为自己的魔法消耗值比例	持续多长时间
        GOTO_WUXING_MAP, //五行奇阵的传送
        GET_WUXING_AWARD, //领取五行奇阵的奖励
        LEAVE_LAOFANG,	//离开牢房
        GOTO_CAISHENMIAO, //进入福神庙	副本的ID
        DB_ADD_ANTIBOSS, //添加BOSS克星	多长时间(单位:分钟)	攻击精英和BOSS怪是增加多少倍的伤害
        RELOAD_COPYMONSTERS, //立刻刷新副本中的怪物	需要的物品ID
        DB_ADD_MONTHVIP, //添加VIP月卡
        INSTALL_JUNQI,	//安插帮旗
        TAKE_SHELIZHIYUAN, //提取舍利之源
        DB_ADD_DBLSKILLUP,	//添加升级技能的双倍的buffer项	多长时间(单位:分钟)
        NEW_JIUHUA_ADDLIFE, //服后可迅速将人物生命值恢复至100%	
        NEW_LIANZHAN_DELAY, //可使连斩获得的BUFF延长60分钟。【此道具不可叠加使用】
        DB_ADD_THREE_EXP, //角色击杀怪物可获得三倍经验值。【此道具可叠加使用，但使用后会替换之前使用的双倍经验卡效果】	多长时间(单位:分钟)
        DB_ADD_THREE_MONEY, //角色击杀怪物可获得三倍的铜钱收益。【此道具可叠加使用，但使用后会冲掉之前使用的双倍铜钱卡效果】	多长时间(单位:分钟)
        DB_ADD_AF_PROTECT,  //添加挂机打怪时的战斗保护buffer项	多长时间(单位:分钟)
        FALL_BAOXIANG,	//掉落的宝箱	掉落ID
        NEW_INSTANT_ATTACK4, //直接物理伤害	原始伤害值要乘以的系数值(浮点数)	每增加一级，增加的伤害值乘以的系数值(浮点数)		
        NEW_INSTANT_MAGIC4, //直接魔法伤害	原始伤害值要乘以的系数值(浮点数)	每增加一级，增加的伤害值乘以的系数值(浮点数)		
        NEW_TIME_MAGIC4, //持续魔法伤害	原始伤害值要乘以的系数值(浮点数)	每增加一级，增加的伤害值乘以的系数值(浮点数)	持续多长时间	总共几次
        NEW_YINLIANG_RNDBAO, //随机银两包	最小值	最大值
        GOTO_LEAVELAOFANG, //离开牢房	消耗的道具ID	
        GOTO_MAPBYGOODS, //传送到某个地图通过扣除某个道具	消耗的道具ID	一次扣除的道具数量
        SUB_ZUIEZHI, //消除罪恶值的公式	减少的罪恶值的点数
        UN_PACK, //解包物品	物品的ID	物品的个数
        GOTO_MAPBYVIP,	//传送到某个地图通过VIP	地图ID vip等级[1,3,6]
        GOTO_BATTLEMAP, //进入决斗场
        FALL_BAOXIANG2, //掉落的宝箱2	掉落ID1(战士)	掉落ID2(法师)	掉落ID3(道士)	最大个数
        GOTO_SHILIANTA,	//进入试练塔副本
        NEW_ADD_GOLD,	//增加金币	增加的金币的值
        GOTO_SHENGXIAOGUESSMAP, //进入生肖竞猜地图
        GOTO_ARENABATTLEMAP,//进入竞技场参加角斗赛
        USE_GOODSFORDLG,//使用物品打开窗口
        DB_ADD_YINYONG, //装备增加天生和强化附加属性
        SUB_PKZHI, //减少PK值	消减的PK数值			
        CALL_MONSTER, //召唤怪物	怪物ID	召唤个数		
        NEW_ADD_JIFEN, //增加装备积分	积分数值			
        NEW_ADD_LIESHA, //增加猎杀数值	数值			
        NEW_ADD_CHENGJIU, //增加成就数值	数值			
        NEW_ADD_WUXING, //增加悟性数值	数值			
        NEW_ADD_ZHENQI, //增加真气数值	数值			
        DB_ADD_TIANSHENG, //增加掉落天生属性buffer	激活天生概率（小数)	持续时间(秒)		
        NEW_PACK_JINGYUAN, //打包天地精元的数量	打包数量			
        ADD_XINGYUN, //给当前佩戴的武器增加幸运值	幸运值			
        FALL_XINGYUN, //根据TongLing.xml配置，随机改变当前佩戴的武器的幸运值				
        NEW_PACK_SHILIAN, //打包试炼令的数量	数值			===>通天令
        DB_NEW_ADD_ZHUFUTIME, //增加安全区获取经验的Buffer	增加时间(秒)			
        NEW_ADD_MAPTIME, //增加限时地图的时间	地图ID	增加的时间(秒)		
        DB_ADD_WAWA_EXP, //增加替身娃娃获取多倍经验的buffer	击杀只数	杀满只数	系数1	系数2
        DB_TIME_LIFE_MAGIC, //增加回复生命值和魔法值的持续类药品公式buffer	生命值	魔法值	持续时间（秒)	回复间隔(秒)
        DB_INSTANT_LIFE_MAGIC, //瞬间回复生命值和魔法值的药品公式	生命值	魔法值		
        DB_ADD_MAXATTACKV, //增加最大物理攻击力的BUFFER	属性值	持续时间(秒)		
        DB_ADD_MAXMATTACKV, //增加最大魔法攻击力的BUFFER	属性值	持续时间(秒)		
        DB_ADD_MAXDSATTACKV, //增加最大道术攻击力的BUFFER	属性值	持续时间(秒)		
        DB_ADD_MAXDEFENSEV, //增加最大最大物理防御的BUFFER	属性值	持续时间(秒)		
        DB_ADD_MAXMDEFENSEV, //增加最大最大魔法防御的BUFFER	属性值	持续时间(秒)		
        OPEN_QIAN_KUN_DAI, //打开乾坤袋挖宝				
        RUN_LUA_SCRIPT, //执行lua脚本	lua脚本的数字ID(放在scripts/run目录下)
	    DB_ADD_EXP,	//在线每分钟获取经验收益公式	经验值	持续时间(秒)	间隔（秒)
        DB_ADD_SEASONVIP, //添加VIP季卡
        DB_ADD_HALFYEARVIP, //添加VIP半年卡
        ADD_BINDYUANBAO,//添加绑定元宝
        GOTO_MINGJIEMAP,//传送到冥界地图 地图编号 持续时间(秒)
        ADD_GUMUMAPTIME,//增加古墓地图时间 持续时间(秒)
        ADD_BOSSCOPYENTERNUM,//增加boss副本进入次数 次数
        GOTO_BOSSCOPYMAP,//进入boss副本 副本id 地图id
        DB_ADD_FIVE_EXP,//角色击杀怪物可获得5倍经验值。【此道具可叠加使用，但使用后会替换之前使用的双倍和三倍经验卡效果】	多长时间(单位:分钟)
        DB_ADD_RANDOM_EXP,//随机经验单 最小经验值,最大经验值
        GOTO_MAPBYYUANBAO,//元宝进入地图 元宝数量 地图id
        ADD_DAILY_NUM,	//增加日常任务的次数	任务类型	增加次数
        DB_TIME_LIFE_NOSHOW,//持续增加生命	每次加多少	持续多长时间	回复间隔秒数
        DB_TIME_MAGIC_NOSHOW, //持续增加魔法	每次加多少	持续多长时间	回复间隔秒数
        GOTO_GUMUMAP,	//进入古墓地图 无参数
        ADD_PKKING_BUFF,//增加pk王buffer 物理攻击 魔法攻击 道术攻击 经验增加倍数
        ADD_DJ,	//添加元宝	元宝值
        DB_ADD_MULTIEXP, //添加多倍经验卡
        RANDOM_SHENQIZHIHUN, //随机获得神奇之魂
        ADD_JIERI_BUFF, //添加节日属性buffer
        DB_ADD_ERGUOTOU, //添加二锅头

        NEW_ADD_ZHANHUN, //添加战魂
        NEW_ADD_RONGYU, //添加荣誉

        EXT_ATTACK_MABI,//麻痹戒指          麻痹效果触发几率,buffer持续时间(秒)
        EXT_RESTORE_BLOOD,//复活戒指        冷却时间(秒)
        EXT_SUB_INJURE,//伤害吸收效果(护身戒指)       伤害吸收百分比(填写0~100整数),吸收1点生命值需要的魔法值

        // 属性改造 增加相关BUFF begin [8/15/2013 LiaoWei]
        DB_ADD_TEMPSTRENGTH,     //持续一段时间内增加角色力量值 (值,持续时间)
        DB_ADD_TEMPINTELLIGENCE, //持续一段时间内增加角色智力值 (值,持续时间)
        DB_ADD_TEMPDEXTERITY,    //持续一段时间内增加角色敏捷值 (值,持续时间)
        DB_ADD_TEMPCONSTITUTION, //持续一段时间内增加角色体力值 (值,持续时间)
        DB_ADD_TEMPATTACKSPEED,  //持续一段时间内增加角色攻击速度值 (值, 持续时间)
        DB_ADD_LUCKYATTACK,      //持续一段时间内增加角色幸运一击的概率 (值, 持续时间)
        DB_ADD_FATALATTACK,      //持续一段时间内增加角色卓越一击的概率 (值, 持续时间)
        DB_ADD_DOUBLEATTACK,      //持续一段时间内增加角色双倍一击的概率 (值, 持续时间)
        DB_ADD_LUCKYATTACKPERCENTTIMER, // 一段时间提升百分比幸运一击效果(百分比,时间)
        DB_ADD_FATALATTACKPERCENTTIMER, // 一段时间提升百分比卓越一击效果(百分比,时间)
        DB_ADD_DOUBLETACKPERCENTTIMER, // 一段时间提升百分比双倍一击效果(百分比,时间)
        DB_ADD_MAXHPVALUE,             // 一段时间提升HP上限(值,时间)
        DB_ADD_MAXMPVALUE,             // 一段时间提升MP上限(值,时间)
        DB_ADD_LIFERECOVERPERCENT,     // 一段时间提示生命恢复百分比(值,时间)
        // 属性改造 增加相关BUFF end [8/15/2013 LiaoWei]
        
        // MU项目技能MagicScripts   begin [10/24/2013 LiaoWei]
        // 说明 -- MU新项目里 1.攻击类型(物理、魔法)不由角色职业决定 而是由技能本身决定 2.增加步长 因为每个技能的数值都要递增

        MU_ADD_PHYSICAL_ATTACK,      // 附加物理攻击 (最小值，最大值，每级值增加的步长)
        MU_ADD_MAGIC_ATTACK,         // 附加魔法攻击 (最小值，最大值，每级值增加的步长)
        MU_SUB_DAMAGE_PERCENT_TIMER, // 一段时间内减少伤害百分比 (时间，百分比，每级值增加的步长)
        MU_ADD_HP_PERCENT_TIMER,     // 一段时间增加百分比的生命值 (时间，百分比，每级值增加的步长)
        MU_ADD_DEFENSE_TIMER,        // 一段时间增加物理和魔法防御力(时间，数值，每级值增加的步长)
        MU_ADD_ATTACK_TIMER,         // 一段时间增加物理和魔法攻击力(时间，数值，每级值增加的步长)
        MU_ADD_HP,                   // 恢复生命值(值，每级增加的步长)
        MU_BLINK_MOVE,               // 瞬间移动--消失X秒 在前方闪现(消失时间，移动距离)
        MU_SUB_DAMAGE_PERCENT_TIMER1, // 一段时间内减少伤害百分比 (时间，百分比，每级值增加的步长)
        MU_RANDOM_SHUXING,           // 随机增加基础属性之一
        MU_RANDOM_STRENGTH,             // 增加力量
        MU_RANDOM_INTELLIGENCE,         // 增加智力
        MU_RANDOM_DEXTERITY,            // 增加敏捷
        MU_RANDOM_CONSTITUTION,         // 增加体力
        MU_ADD_PHYSICAL_ATTACK1,        // 附加物理攻击 (伤害基础比例,固定伤害加成值)
        MU_ADD_ATTACK_DOWN,             // 对目标造成物理伤害,额外附加物理伤害,并有概率使对方攻击下降30%,持续2秒 (触发概率,下降比例,持续时间)
        MU_ADD_HUNMI,                   // 对目标造成物理伤害,额外附加物理伤害,并有概率使对方昏迷,持续2秒(触发概率,持续时间)
        MU_ADD_MOVESPEED_DOWN,          // 对目标造成物理伤害,额外附加物理伤害,并有概率使对方移动速度减少30%,持续3秒 (触发概率,减少比例,持续时间)
        MU_ADD_LIFE,                    // 提升人物的生命上限,额外附加生命上限,持续秒 (生命上限基础比例,固定生命上限值,持续时间)
        MU_ADD_MAGIC_ATTACK1,           // 对目标造成魔法伤害,额外附加魔法伤害,连击数越高,伤害提升越明显(伤害基础比例,固定伤害加成值)
        MU_ADD_HIT_DOWN,                // 有概率使对方移动速度减少30%,持续3秒 (触发概率,减少比例,持续时间)
        MU_SUB_DAMAGE_PERCENT,          // 一段时间内减少伤害百分比 (伤害减免基础比例,固定伤害减免值,持续时间))
        MU_SUB_DAMAGE_VALUE,            // 一段时间内减少伤害值 (时间, 值)
        MU_ADD_DEFENSE_DOWN,            // 一段时间内减少防御(触发概率,下降比例,持续时间)
        MU_ADD_DEFENSE_ATTACK,          // 一段时间内提升攻击、防御 (攻防提升基础比例,攻防固定提升值,持续时间)
        MU_ADD_JITUI,                   // 击飞(触发概率,距离值)
        MU_ADD_DINGSHENG,               // 定身
        MU_ADD_HIT_DODGE, // 一段时间内提升命中、闪避 (命中闪避提升基础比例,命中闪避固定提升值,持续时间) [XSea 2015/5/12]
        // MU项目技能MagicScripts   end [10/24/2013 LiaoWei]

        GOTO_BLOODCASTLE,           // 进入血色堡垒 [11/6/2013 LiaoWei] [2015-9-11废弃]
        GET_AWARD_BLOODCASTLE,      // 领取血色堡垒奖励 [11/6/2013 LiaoWei] [2015-9-11废弃]

        GOTO_DAIMONSQUARE,           // 进入恶魔广场 [12/25/2013 LiaoWei] [2015-9-11废弃]
        GOTO_ANGELTEMPLE,           // 进入天使神殿 [3/23/2014 LiaoWei]
        SCAN_SQUARE,     //矩形扫描
        FRONT_SECTOR, //攻击扇形区域， 参数: 角度
        ROUNDSCAN,      // 圆形扫描

        OPEN_TREASURE_BOX, // 开宝箱 [1/7/2014 LiaoWei]

        GOTO_BOOSZHIJIA,        // 进入BOOS之家 [3/29/2014 LiaoWei]
        GOTO_HUANGJINSHENGDIAN, // 进入黄金圣殿之家 [3/29/2014 LiaoWei]

        ADD_VIPEXP, // 增加VIP经验值 [4/6/2014 LiaoWei]

        ADD_SHENGWANG, // 增加声望值 [5/8/2014 LiaoWei]

        GET_AWARD_BLOODCASTLECOPYSCENE, // 领取副本的血色城堡奖励 [7/7/2014 LiaoWei]

        ADDMONSTERSKILL,  //为怪物添加技能
        REMOVEMONSTERSKILL,  //为怪物删除技能
        BOSS_CALLMONSTERONE, //boss召唤怪物1
        BOSS_CALLMONSTERTWO, //boss召唤怪物2
        CLEAR_MONSTER_BUFFERID, //清楚指定的怪物的bufferID
        ADD_XINGHUN, // 增加星魂值 [8/4/2014 LiaoWei]
        UP_LEVEL, // 提示等级物品 [8/12/2014 LiaoWei]
        ADD_GUANGMUI,       //添加光幕
        CLEAR_GUANGMUI,     //清除光幕

        FEIXUE,             //腐蚀沸血
        ZHONGDU,            //毒爆术
        LINGHUN,            //灵魂奔腾
        RANSHAO,            //生命燃烧
        HUZHAO,             //重生
        WUDIHUZHAO,         //无敌

        MU_FIRE_WALL1, //目标1*1范围内魔法伤害(伤害间隔,伤害次数,伤害百分比,固定附加值)
        MU_FIRE_WALL9, //目标3*3范围内魔法伤害(伤害间隔,伤害次数,伤害百分比,固定附加值)
        MU_FIRE_WALL25, //目标5*5范围内魔法伤害(伤害间隔,伤害次数,伤害百分比,固定附加值)
        MU_FIRE_WALL_X, //目标半径X范围内魔法伤害(伤害间隔,伤害次数,伤害百分比,固定附加值,半径)
        MU_FIRE_SECTOR, //目标扇形范围内魔法伤害(伤害间隔,伤害次数,伤害百分比,固定附加值, 半径, 角度)
        MU_FIRE_STRAIGHT, //前方X范围内魔法伤害(伤害间隔,伤害次数,伤害百分比,固定附加值, 距离, 宽度)
        MU_FIRE_WALL_ACTION, //目标半径X范围内定身效果(伤害间隔,伤害次数,半径,效果公式,效果公式参数1,...)

        BOSS_ADDANIMATION, //Boss相关动画(首领ID,所在地图ID,动画ID, 位置x,位置y,动画位置x,动画位置y)
        ADDYSFM,    // 给玩家增加元素粉末
        ADD_LINGJING,     //增加灵晶
        ADD_ZAIZAO,//增加再造点
        ADD_GOODWILL,   //增加婚戒友善度
        MU_ADD_MOVE_SPEED_DOWN, // 攻击触发减速效果
        ADD_RONGYAO,   //增加(天梯)荣耀值
        MU_ADD_PALSY, // 攻击触发麻痹效果
        MU_ADD_FROZEN, // 攻击触发冰冻效果
        MU_GETSHIZHUANG,    //激活道具ID对应的时装 panghui add
        
        //[bing] 圣物系统新加 2015.6.17
        POTION,             //药水效果：Potion，百分比 药水：GoodsID=1010、1011、1012、1013、1110、1111 效果：基础效果（1+ X.X）
        HOLYWATER,          //圣水效果：HolyWater，百分比 圣水：GoodsID=1000、1001、1002、1100、1101、1102 效果：基础效果（1+ X.X）
        RECOVERLIFEV,       //自动恢复生命效果：RecoverLifeV，百分比 效果：基础恢复生命效果*（1+X.X）
        RECOVERMAGICV,      //自动恢复魔法效果：RecoverMagicV，百分比 效果：基础恢复魔法效果+X.X
        LIFESTEAL,         //击中恢复效果：LifeStealV，固定值 效果：击中恢复生命+XX
        LIFESTEALPERCENT,   //击中恢复效果：LifeStealPercent，百分比 效果：击中恢复生命*（1+X.X）
        FATALHURT,          //卓越伤害加成：FatalHurt，百分比 效果：卓越一击伤害加成*（1+X.X）

        ADDATTACK,          //对应SystemMagicAction属性枚举
        ADDATTACKINJURE,
        HITV,
        ADDDEFENSE,
        COUNTERACTINJUREVALUE,
        DAMAGETHORN,
        DODGE,
        MAXLIFEPERCENT,

        STRENGTH,
        CONSTITUTION,
        DEXTERITY,
        INTELLIGENCE,

        ADD_SHENGWU,        //随机得到24个部位的碎片
        ADD_SHENGBEI,       //随机得到圣杯碎片
        ADD_SHENGJIAN,      //随机得到圣剑碎片
        ADD_SHENGGUAN,      //随机得到圣冠碎片
        ADD_SHENGDIAN,      //随机得到圣典碎片

		ADD_GUARDPOINT, //增加守护点

		NEW_ADD_YINGGUANG, // 随机得到荧光粉末(X,Y) [XSea 2015/8/17]

        AddAttackPercent,
        AddDefensePercent,

        WOLF_SEARCH_ROAD,//狼魂要塞——寻路
        WOLF_ATTACK_ROLE,//狼魂要塞——攻击角色
        SELF_BURST = 318,//狼魂要塞——自爆
        ADD_BANGGONG, //添加战盟战功(帮贡)
        ADD_LANGHUN, // 增加狼魂粉末
        MU_ADD_PROPERTY, // 增加二级属性,值为基础值*技能等级,公式(二级属性类型,基础值,持续时间秒)

        ADD_ZHENGBADIANSHU, // 增加争霸点书
        ADD_WANGZHEDIANSHU, // 王者点
        MAX, //最后一个    
    }

    /// <summary>
    /// 公式定义
    /// </summary>
    public class MagicActionItem
    {
        /// <summary>
        /// 公式ID
        /// </summary>
        public MagicActionIDs MagicActionID
        {
            get;
            set;
        }

        /// <summary>
        /// 公式参数
        /// </summary>
        public double[] MagicActionParams
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 技能公式管理
    /// </summary>
    public class SystemMagicAction
    {
        #region 公式定义

        static SystemMagicAction()
        {
            for (MagicActionIDs id = 0; id < MagicActionIDs.MAX; id++)
            {
                string name = id.ToString().ToLower();
                MagicActionIDsDict.Add(name, id);
            }
        }

        private static Dictionary<string, MagicActionIDs> MagicActionIDsDict = new Dictionary<string, MagicActionIDs>();

        private static void PrintMaigcActionDictUsage(string name, Dictionary<string, MagicActionIDs> dict)
        {
            Console.WriteLine(string.Format("{0}个数{1}", name, dict.Count));
            foreach (var kv in dict)
            {
                Console.WriteLine(string.Format("{0} {1}", kv.Key, kv.Value));
            }
            Console.WriteLine("\r\n");
        }

        public static void PrintMaigcActionUsage()
        {
            PrintMaigcActionDictUsage("MagicActionIDsDict", MagicActionIDsDict);
        }

        /// <summary>
        /// 根据名称查找ID
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private int FindIDByName(string name)
        {
            MagicActionIDs id;
            if (MagicActionIDsDict.TryGetValue(name.ToLower(), out id))
            {
                return (int)id;
            }

            return -1;
        }

        /// <summary>
        /// 解析公式参数
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private MagicActionItem ParseParams(string item)
        {
            string name = "";
            string paramsList = "";
            int start = item.IndexOf('(');
            if (-1 != start)
            {
                int end = item.IndexOf(')', start + 1);
                if (-1 == end) return null;

                name = item.Substring(0, start);
                paramsList = item.Substring(start + 1, end - start - 1);
            }
            else if ((start = item.IndexOf(',')) != -1)
            {
                name = item.Substring(0, start);
                paramsList = item.Substring(start + 1, item.Length - start - 1);
            }
            else
            {
                name = item;
                paramsList = "";
            }

            int id = FindIDByName(name);
            if (id < 0)
            {
                return null;
            }

            double[] actionParams = null;
            if (paramsList != "")
            {
                string[] paramsArray = paramsList.Split(',');
                actionParams = new double[paramsArray.Length];
                for (int i = 0; i < paramsArray.Length; i++)
                {
                    if (char.IsDigit(paramsArray[i], 0))
                    {
                        actionParams[i] = Global.SafeConvertToDouble(paramsArray[i]);
                    }
                    else
                    {
                        actionParams[i] = FindIDByName(paramsArray[i]);
                    }
                }
            }

            return new MagicActionItem()
            {
                MagicActionID = (MagicActionIDs)id,
                MagicActionParams = actionParams,
            };
        }

        /// <summary>
        /// 解析公式字符串
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        private List<MagicActionItem> ParseActions(string actions)
        {
            List<MagicActionItem> itemsList = new List<MagicActionItem>();
            string[] actionFields = actions.Split('|');
            for (int j = 0; j < actionFields.Length; j++)
            {
                string item = actionFields[j].Trim();
                MagicActionItem magicActionItem = ParseParams(item);
                if (null != magicActionItem)
                {
                    itemsList.Add(magicActionItem);
                }
            }

            return itemsList;
        }

        /// <summary>
        /// 解析公式
        /// </summary>
        /// <param name="id"></param>
        /// <param name="actions"></param>
        private void ParseMagicActions(Dictionary<int, List<MagicActionItem>> dict, int id, string actions)
        {
            actions = actions.Trim();
            if ("" == actions) return;

            List<MagicActionItem> magicActionList = ParseActions(actions);
            dict[id] = magicActionList;
        }

        /// <summary>
        /// 解析公式  [bing] 提供给外部List使用
        /// </summary>
        /// <param name="id"></param>
        /// <param name="actions"></param>
        public List<MagicActionItem> ParseActionsOutUse(string strAction)
        {
            return ParseActions(strAction);
        }

        #endregion 公式定义

        #region 技能公式使用

        /// <summary>
        /// 公式索引词典
        /// </summary>
        private Dictionary<int, List<MagicActionItem>> _MagicActionsDict = null;

        /// <summary>
        /// 公式索引词典
        /// </summary>
        public Dictionary<int, List<MagicActionItem>> MagicActionsDict
        {
            get { return _MagicActionsDict; }
        }

        /// <summary>
        /// 解析公式
        /// </summary>
        /// <param name="systemMagicsMgr"></param>
        public void ParseMagicActions(SystemXmlItems systemMagicsMgr)
        {
            Dictionary<int, List<MagicActionItem>> magicActionsDict = new Dictionary<int, List<MagicActionItem>>();
            foreach (var key in systemMagicsMgr.SystemXmlItemDict.Keys)
            {
                string actions = (string)systemMagicsMgr.SystemXmlItemDict[(int)key].GetStringValue("MagicScripts");
                if (null == actions) continue;
                ParseMagicActions(magicActionsDict, (int)key, actions);
            }

            _MagicActionsDict = magicActionsDict;
        }

        /// <summary>
        /// 解析公式
        /// </summary>
        /// <param name="systemMagicsMgr"></param>
        public void ParseMagicActions2(SystemXmlItems systemMagicsMgr)
        {
            //Dictionary<int, List<MagicActionItem>> magicActionsDict = new Dictionary<int, List<MagicActionItem>>();
            //foreach (var key in systemMagicsMgr.SystemXmlItemDict.Keys)
            //{
            //    string actions = (string)systemMagicsMgr.SystemXmlItemDict[(int)key].GetStringValue("MagicScripts2");
            //    if (null == actions) continue;
            //    ParseMagicActions(magicActionsDict, (int)key, actions);
            //}

            //_MagicActionsDict = magicActionsDict;
        }

        /// <summary>
        /// 解析攻击范围公式
        /// </summary>
        /// <param name="systemMagicsMgr"></param>
        public void ParseScanTypeActions2(SystemXmlItems systemMagicsMgr)
        {
            Dictionary<int, List<MagicActionItem>> magicActionsDict = new Dictionary<int, List<MagicActionItem>>();
            foreach (var key in systemMagicsMgr.SystemXmlItemDict.Keys)
            {
                string actions = (string)systemMagicsMgr.SystemXmlItemDict[(int)key].GetStringValue("ScanType");
                if (null == actions) continue;
                ParseMagicActions(magicActionsDict, (int)key, actions);
            }

            _MagicActionsDict = magicActionsDict;
        }

        #endregion 技能公式使用

        #region 物品公式使用

        /// <summary>
        /// 公式索引词典
        /// </summary>
        private Dictionary<int, List<MagicActionItem>> _GoodsActionsDict = null;

        /// <summary>
        /// 公式索引词典
        /// </summary>
        public Dictionary<int, List<MagicActionItem>> GoodsActionsDict
        {
            get { return _GoodsActionsDict; }
        }

        /// <summary>
        /// 解析公式
        /// </summary>
        /// <param name="systemMagicsMgr"></param>
        public void ParseGoodsActions(SystemXmlItems systemGoodsMgr)
        {
            Dictionary<int, List<MagicActionItem>> goodsActionsDict = new Dictionary<int, List<MagicActionItem>>();
            foreach (var key in systemGoodsMgr.SystemXmlItemDict.Keys)
            {
                //int execScript = (int)systemGoodsMgr.SystemXmlItemDict[(int)key].GetIntValue("ExecScript");
                //if (execScript <= 0) continue;
                string actions = (string)systemGoodsMgr.SystemXmlItemDict[(int)key].GetStringValue("ExecMagic");
                if (string.IsNullOrEmpty(actions)) continue;
                ParseMagicActions(goodsActionsDict, (int)key, actions);
            }

            _GoodsActionsDict = goodsActionsDict;
        }

        #endregion 物品公式使用

        #region NPC功能脚本

        /// <summary>
        /// 公式索引词典
        /// </summary>
        private Dictionary<int, List<MagicActionItem>> _NPCScriptActionsDict = null;

        /// <summary>
        /// 公式索引词典
        /// </summary>
        public Dictionary<int, List<MagicActionItem>> NPCScriptActionsDict
        {
            get { return _NPCScriptActionsDict; }
        }

        /// <summary>
        /// 解析公式
        /// </summary>
        /// <param name="systemMagicsMgr"></param>
        public void ParseNPCScriptActions(SystemXmlItems systemNPCScripts)
        {
            Dictionary<int, List<MagicActionItem>> npcScriptActionsDict = new Dictionary<int, List<MagicActionItem>>();            
            foreach (var key in systemNPCScripts.SystemXmlItemDict.Keys)
            {
                string actions = (string)systemNPCScripts.SystemXmlItemDict[(int)key].GetStringValue("ExecMagic");
                if (null == actions) continue;
                ParseMagicActions(npcScriptActionsDict, (int)key, actions);
            }

            _NPCScriptActionsDict = npcScriptActionsDict;
        }

        #endregion NPC功能脚本

        #region BossAI公式使用

        /// <summary>
        /// 公式索引词典
        /// </summary>
        private Dictionary<int, List<MagicActionItem>> _BossAIActionsDict = null;

        /// <summary>
        /// 公式索引词典
        /// </summary>
        public Dictionary<int, List<MagicActionItem>> BossAIActionsDict
        {
            get { return _BossAIActionsDict; }
        }

        /// <summary>
        /// 解析公式
        /// </summary>
        /// <param name="systemMagicsMgr"></param>
        public void ParseBossAIActions(SystemXmlItems systemBossAI)
        {
            Dictionary<int, List<MagicActionItem>> bossAIActionsDict = new Dictionary<int, List<MagicActionItem>>();
            foreach (var key in systemBossAI.SystemXmlItemDict.Keys)
            {
                //int execScript = (int)systemGoodsMgr.SystemXmlItemDict[(int)key].GetIntValue("ExecScript");
                //if (execScript <= 0) continue;
                string actions = (string)systemBossAI.SystemXmlItemDict[(int)key].GetStringValue("Action");
                if (string.IsNullOrEmpty(actions)) continue;
                ParseMagicActions(bossAIActionsDict, (int)key, actions);
            }

            _BossAIActionsDict = bossAIActionsDict;
        }

        #endregion BossAI公式使用


        #region 拓展属性公式使用

        /// <summary>
        /// 公式索引词典
        /// </summary>
        private Dictionary<int, List<MagicActionItem>> _ExtensionPropsActionsDict = null;

        /// <summary>
        /// 公式索引词典
        /// </summary>
        public Dictionary<int, List<MagicActionItem>> ExtensionPropsActionsDict
        {
            get { return _ExtensionPropsActionsDict; }
        }

        /// <summary>
        /// 解析公式
        /// </summary>
        /// <param name="systemMagicsMgr"></param>
        public void ParseExtensionPropsActions(SystemXmlItems systemExtensionProps)
        {
            Dictionary<int, List<MagicActionItem>> extensionPropsActionsDict = new Dictionary<int, List<MagicActionItem>>();
            foreach (var key in systemExtensionProps.SystemXmlItemDict.Keys)
            {
                string actions = (string)systemExtensionProps.SystemXmlItemDict[(int)key].GetStringValue("MagicScripts");
                if (string.IsNullOrEmpty(actions)) continue;
                ParseMagicActions(extensionPropsActionsDict, (int)key, actions);
            }

            _ExtensionPropsActionsDict = extensionPropsActionsDict;
        }

        #endregion 拓展属性公式使用
    }
}
