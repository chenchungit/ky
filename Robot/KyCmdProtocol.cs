//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Option: missing-value detection (*Specified/ShouldSerialize*/Reset*) enabled
    
// Generated from: proto/KyCmdProtocol.proto
namespace CC
{
    [global::ProtoBuf.ProtoContract(Name=@"CommandID")]
    public enum CommandID
    {
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_DEFAUL", Value=0)]
      CMD_DEFAUL = 0,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_CREATE_ROLE", Value=30002)]
      CMD_CREATE_ROLE = 30002,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_GET_ROLE_LIST", Value=30003)]
      CMD_GET_ROLE_LIST = 30003,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_INIT_GAME", Value=30004)]
      CMD_INIT_GAME = 30004,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_PLAY_GAME", Value=30005)]
      CMD_PLAY_GAME = 30005,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_GAME_MOVE", Value=30006)]
      CMD_GAME_MOVE = 30006,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_STOP_MOVE", Value=30007)]
      CMD_STOP_MOVE = 30007,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_POSITION_MOVE", Value=30008)]
      CMD_POSITION_MOVE = 30008,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_MAP_CHANGE", Value=30009)]
      CMD_MAP_CHANGE = 30009,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_DB_UPDATE_POS", Value=30010)]
      CMD_DB_UPDATE_POS = 30010,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_MAP_LEAVE", Value=30011)]
      CMD_MAP_LEAVE = 30011,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_OTHER_ROLE", Value=30012)]
      CMD_OTHER_ROLE = 30012,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_SYSTEM_MONSTER", Value=30013)]
      CMD_SYSTEM_MONSTER = 30013,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_PLAY_ATTACK", Value=30014)]
      CMD_PLAY_ATTACK = 30014,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_DB_ADD_Skill", Value=30015)]
      CMD_DB_ADD_Skill = 30015,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_READY_ATTACK", Value=30016)]
      CMD_READY_ATTACK = 30016,
            
      [global::ProtoBuf.ProtoEnum(Name=@"CMD_MONSTER_DEAD", Value=30017)]
      CMD_MONSTER_DEAD = 30017
    }
  
    [global::ProtoBuf.ProtoContract(Name=@"ErrorCode")]
    public enum ErrorCode
    {
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_OK", Value=0)]
      ERROR_OK = 0,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_DATA_LIMIT", Value=1)]
      ERROR_DATA_LIMIT = 1,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_NAME_INVALCHARACTER", Value=2)]
      ERROR_NAME_INVALCHARACTER = 2,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_NAME_LENGTH_LIMIT", Value=3)]
      ERROR_NAME_LENGTH_LIMIT = 3,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_REG_FAIL", Value=4)]
      ERROR_REG_FAIL = 4,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_CREATE_ROLE_LIMIT", Value=5)]
      ERROR_CREATE_ROLE_LIMIT = 5,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_PROTOCOL", Value=6)]
      ERROR_PROTOCOL = 6,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_NAME_EXIST", Value=7)]
      ERROR_NAME_EXIST = 7,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_CREATE_ROLE_FAIL", Value=8)]
      ERROR_CREATE_ROLE_FAIL = 8,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_INVALID_USERID", Value=9)]
      ERROR_INVALID_USERID = 9,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_PROHIBIT_LOGIN", Value=10)]
      ERROR_PROHIBIT_LOGIN = 10,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_INVALID_ROLE", Value=11)]
      ERROR_INVALID_ROLE = 11,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_DELING_ROLE", Value=12)]
      ERROR_DELING_ROLE = 12,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_INITGAME_FAIL", Value=13)]
      ERROR_INITGAME_FAIL = 13,
            
      [global::ProtoBuf.ProtoEnum(Name=@"ERROR_ADDSKILL_FAIL", Value=14)]
      ERROR_ADDSKILL_FAIL = 14
    }
  
}