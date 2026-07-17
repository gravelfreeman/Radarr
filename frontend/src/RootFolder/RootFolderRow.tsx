import React, { useCallback, useState } from 'react';
import { useDispatch } from 'react-redux';
import CheckInput from 'Components/Form/CheckInput';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { icons, kinds } from 'Helpers/Props';
import { deleteRootFolder } from 'Store/Actions/rootFolderActions';
import { CheckInputChanged } from 'typings/inputs';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './RootFolderRow.css';

interface RootFolderRowProps {
  id: number;
  path: string;
  originalRecycleBinEnabled: boolean;
  recycleBinEnabled: boolean;
  accessible: boolean;
  freeSpace?: number;
  unmappedFolders: object[];
  onRecycleBinEnabledChange?: (
    id: number,
    recycleBinEnabled: boolean,
    originalRecycleBinEnabled: boolean
  ) => void;
}

function RootFolderRow(props: RootFolderRowProps) {
  const {
    id,
    path,
    originalRecycleBinEnabled,
    recycleBinEnabled,
    accessible,
    freeSpace = 0,
    unmappedFolders = [],
    onRecycleBinEnabledChange,
  } = props;

  const isUnavailable = !accessible;

  const dispatch = useDispatch();

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);

  const onDeletePress = useCallback(() => {
    setIsDeleteModalOpen(true);
  }, [setIsDeleteModalOpen]);

  const onDeleteModalClose = useCallback(() => {
    setIsDeleteModalOpen(false);
  }, [setIsDeleteModalOpen]);

  const onConfirmDelete = useCallback(() => {
    dispatch(deleteRootFolder({ id }));

    setIsDeleteModalOpen(false);
  }, [dispatch, id]);

  const onRecycleBinEnabledInputChange = useCallback(
    ({ value }: CheckInputChanged) => {
      onRecycleBinEnabledChange?.(id, value, originalRecycleBinEnabled);
    },
    [id, originalRecycleBinEnabled, onRecycleBinEnabledChange]
  );

  return (
    <TableRow>
      <TableRowCell>
        {isUnavailable ? (
          <div className={styles.unavailablePath}>
            {path}

            <Label className={styles.unavailableLabel} kind={kinds.DANGER}>
              {translate('Unavailable')}
            </Label>
          </div>
        ) : (
          <Link className={styles.link} to={`/add/import/${id}`}>
            {path}
          </Link>
        )}
      </TableRowCell>

      <TableRowCell className={styles.freeSpace}>
        {isUnavailable || isNaN(Number(freeSpace))
          ? '-'
          : formatBytes(freeSpace)}
      </TableRowCell>

      <TableRowCell className={styles.unmappedFolders}>
        {isUnavailable ? '-' : unmappedFolders.length}
      </TableRowCell>

      <TableRowCell className={styles.recycleBinEnabled}>
        <CheckInput
          name={`recycleBinEnabled-${id}`}
          value={recycleBinEnabled}
          onChange={onRecycleBinEnabledInputChange}
        />
      </TableRowCell>

      <TableRowCell className={styles.actions}>
        <IconButton
          title={translate('RemoveRootFolder')}
          name={icons.REMOVE}
          onPress={onDeletePress}
        />
      </TableRowCell>

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        kind={kinds.DANGER}
        title={translate('RemoveRootFolder')}
        message={translate('RemoveRootFolderMoviesMessageText', { path })}
        confirmLabel={translate('Remove')}
        onConfirm={onConfirmDelete}
        onCancel={onDeleteModalClose}
      />
    </TableRow>
  );
}

export default RootFolderRow;
