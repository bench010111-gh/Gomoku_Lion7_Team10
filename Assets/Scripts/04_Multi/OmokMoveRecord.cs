using System;
using System.Collections.Generic;

[Serializable]
public class OmokMoveRecord
{
    public int turnIndex;
    public int x;
    public int y;
    public int stone;
}

[Serializable]
public class OmokMoveRecordList
{
    public List<OmokMoveRecord> moves = new List<OmokMoveRecord>();
}