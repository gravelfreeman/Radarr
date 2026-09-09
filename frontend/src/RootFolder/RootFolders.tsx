import React, { useCallback, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { kinds } from 'Helpers/Props';
import { fetchRootFolders } from 'Store/Actions/rootFolderActions';
import createRootFoldersSelector from 'Store/Selectors/createRootFoldersSelector';
import { InputOnChange } from 'typings/inputs';
import translate from 'Utilities/String/translate';
import RootFolderRow from './RootFolderRow';

const rootFolderColumns: Column[] = [
  {
    name: 'path',
    label: () => translate('Path'),
    isVisible: true,
  },
  {
    name: 'freeSpace',
    label: () => translate('FreeSpace'),
    isVisible: true,
  },
  {
    name: 'unmappedFolders',
    label: () => translate('UnmappedFolders'),
    isVisible: true,
  },
  {
    name: 'recycleBinEnabled',
    label: () => translate('RecyclingBin'),
    isVisible: true,
  },
  {
    name: 'actions',
    label: '',
    isVisible: true,
  },
];

const rootFolderColumnsWithoutRecycleBin = rootFolderColumns.filter(
  (column) => column.name !== 'recycleBinEnabled'
);

export type RootFolderUpdate = {
  id: number;
  recycleBinEnabled: boolean;
};

const EMPTY_ROOT_FOLDER_UPDATES: RootFolderUpdate[] = [];

interface RootFoldersProps {
  rootFolderUpdates?: RootFolderUpdate[] | null;
  onInputChange?: InputOnChange<RootFolderUpdate[] | null>;
}

function RootFolders(props: RootFoldersProps) {
  const { rootFolderUpdates: pendingUpdates, onInputChange } = props;
  const rootFolderUpdates = pendingUpdates ?? EMPTY_ROOT_FOLDER_UPDATES;
  const { isFetching, isPopulated, error, items } = useSelector(
    createRootFoldersSelector()
  );

  const rootFolderUpdatesById = rootFolderUpdates.reduce((result, update) => {
    result[update.id] = update;
    return result;
  }, {} as Record<number, RootFolderUpdate>);

  const onRecycleBinChange = useCallback(
    (id: number, recycleBinEnabled: boolean) => {
      const updates = rootFolderUpdates.filter((update) => update.id !== id);
      const rootFolder = items.find((item) => item.id === id);

      if (rootFolder?.recycleBinEnabled !== recycleBinEnabled) {
        updates.push({ id, recycleBinEnabled });
      }

      onInputChange?.({
        name: 'rootFolderUpdates',
        value: updates.length > 0 ? updates : null,
      });
    },
    [items, onInputChange, rootFolderUpdates]
  );

  const dispatch = useDispatch();

  useEffect(() => {
    dispatch(fetchRootFolders());
  }, [dispatch]);

  if (isFetching && !isPopulated) {
    return <LoadingIndicator />;
  }

  if (!isFetching && !!error) {
    return (
      <Alert kind={kinds.DANGER}>{translate('RootFoldersLoadError')}</Alert>
    );
  }

  return (
    <Table
      columns={
        onInputChange ? rootFolderColumns : rootFolderColumnsWithoutRecycleBin
      }
    >
      <TableBody>
        {items.map((rootFolder) => {
          return (
            <RootFolderRow
              key={rootFolder.id}
              id={rootFolder.id}
              path={rootFolder.path}
              recycleBinEnabled={
                rootFolderUpdatesById[rootFolder.id]?.recycleBinEnabled ??
                rootFolder.recycleBinEnabled
              }
              accessible={rootFolder.accessible}
              freeSpace={rootFolder.freeSpace}
              unmappedFolders={rootFolder.unmappedFolders}
              onRecycleBinChange={
                onInputChange ? onRecycleBinChange : undefined
              }
            />
          );
        })}
      </TableBody>
    </Table>
  );
}

export default RootFolders;
