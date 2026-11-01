import ModelBase from 'App/ModelBase';

interface RootFolder extends ModelBase {
  id: number;
  path: string;
  recycleBinEnabled: boolean;
  accessible: boolean;
  freeSpace?: number;
  unmappedFolders: object[];
}

export default RootFolder;
