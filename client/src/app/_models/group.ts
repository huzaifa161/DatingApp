export interface Group {
  connections: Connection[];
}

interface Connection {
  connectionId: string;
  username: string;
}
