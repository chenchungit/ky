-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- ����Ա
function talk(luaMgr, client, params)
  return '<span class="title">���е�ͼ��</span>\n'
		..'<span class="chuansong">'		
       ..'<a href="event:tomap1">�� �� ��[1�� ]</a>    <a href="event:tomap2">��    ��[15��]</a>\n'
       ..'<a href="event:tomap6">����֮Ұ[30��]</a>    <a href="event:tomap1011">�� �� ��[15��]</a>\n'
		..'</span>\n'	
		 
       ..'<span class="title">������ͼ��</span>\n'
			 ..'<span class="chuansong">'
       ..'<a href="event:tomap3">�� �� ��[20��]</a>    <a href="event:tomap4">Ѫ�ֽ���[25��]</a>\n'
       ..'<a href="event:tomap5">Ī а ��[30��]</a>    <a href="event:tomap7">�������[32��]</a>\n'
       ..'<a href="event:tomap8">������ʥ[38��]</a>    <a href="event:tomap9">����ħ��[44��]</a>\n'
       ..'<a href="event:tomap10">��������[50��]</a>    <a href="event:tomap11">������ԭ[60��]</a>\n'
       ..'<a href="event:tomap12">�������[70��]</a>    <a href="event:tomap13">�� �� ��[80��]</a>\n'
       ..'<a href="event:_tomap14">ʥӰħѨ[90��]</a>   <a href="event:_tomap15">�����ɲ[100��]</a>\n'
			 ..'</span>\n'

	..'<span class="title">�ڿ��ͼ��</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:tomap30">��</a>\n'
		..'</span>\n'
end

function tomap1(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 1)
end

function tomap2(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 15, 2)
end

function tomap6(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 30, 6)
end

function tomap1011(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 15, 1011)
end

-- ���� 35��
function tomap4400(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 35, 4400)
end

function tomap3(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 20, 3)
end

function tomap4(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 25, 4)
end

function tomap5(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 30, 5)
end

function tomap7(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 32, 7)
end

function tomap8(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 38, 8)
end

function tomap9(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 40, 9)
end

function tomap10(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 45, 10)
end

function tomap11(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 50, 11)
end

function tomap12(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 50, 12)
end

function tomap13(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 50, 13)
end

function _tomap14(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 60, 14)
end

function _tomap15(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 60, 15)
end

function tomap30(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 40, 30)
end

-- ���ʹ���
function checkAndLetGo(luaMgr, client, level, mapid)
     if (not checkLevel(luaMgr, client, level)) then
        luaMgr:Error(client, '��ɫ�ȼ�����'..level..'�����ܴ���')
	return 'keepopen'
     end

     luaMgr:GotoMap(client, mapid)
     return 'closewindow'
end

-- ����ɫ�㼶
function checkLevel(luaMgr, client, level)
     if (luaMgr:GetRoleLevel(client) < level) then
        return false
     end

     return true
end
