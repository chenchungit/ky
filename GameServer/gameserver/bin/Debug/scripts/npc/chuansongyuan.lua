-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- ����Ա
function talk(luaMgr, client, params)
  return '<span class="title">���е�ͼ��</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:_tomap1">�� �� ��</a>    <a href="event:_tomap2">��    ��</a>    <a href="event:_tomap6">����֮Ұ</a>\n'
       ..'<a href="event:_tomap1011">�� �� ��</a>    <a href="event:_tomap4400">��    ��</a>\n'
		..'</span>\n'

       ..'<span class="title">������ͼ��</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:_tomap3">�� �� ��</a>    <a href="event:_tomap4">Ѫ�ֽ���</a>    <a href="event:_tomap5">Ī а ��</a>\n'
       ..'<a href="event:_tomap7">�������</a>    <a href="event:_tomap8">������ʥ</a>    <a href="event:_tomap9">����ħ��</a>\n'
       ..'<a href="event:_tomap10">��������</a>    <a href="event:_tomap11">������ԭ</a>    <a href="event:_tomap12">�������</a>\n'
       ..'<a href="event:_tomap13">�� �� ��</a>    <a href="event:_tomap14">ʥӰħѨ</a>    <a href="event:_tomap15">�����ɲ</a>\n'
		..'</span>\n'	

       ..'<span class="title">װ����ͼ��</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:_tomap6000">Ѫ��ħ1��</a>    <a href="event:_tomap6001">�񽣹㳡</a>    <a href="event:_tomap6002">������Ԩ</a>\n\n'
		..'</span>\n'

	..'<span class="title">�ڿ��ͼ��</span>\n'
		..'<span class="chuansong">'
       ..'<a href="event:tomap30">��</a>\n'
		..'</span>\n'

end

function _tomap1(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 1, 1)
end

function _tomap2(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 15, 2)
end

function _tomap6(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 30, 6)
end

function _tomap1011(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 15, 1011)
end

-- ���� 35��
function _tomap4400(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 35, 4400)
end

function _tomap3(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 20, 3)
end

function _tomap4(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 25, 4)
end

function _tomap5(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 30, 5)
end

function _tomap7(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 32, 7)
end

function _tomap8(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 38, 8)
end

function _tomap9(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 40, 9)
end

function _tomap10(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 45, 10)
end

function _tomap11(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 50, 11)
end

function _tomap12(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 50, 12)
end

function _tomap13(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 50, 13)
end

function _tomap14(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 60, 14)
end


function _tomap15(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 60, 15)
end

function _tomap6000(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 35, 6000)
end

function _tomap6001(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 40, 6001)
end

function _tomap6002(luaMgr, client, params)
     return checkAndLetGo(luaMgr, client, 45, 6002)
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