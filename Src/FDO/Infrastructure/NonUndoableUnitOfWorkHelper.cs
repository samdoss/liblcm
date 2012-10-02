// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NonUndoableUnitOfWorkHelper.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// Class that starts and ends a non-undoable Unit Of Work...not necessarily on the undo stack passed,
	/// but on the non-undoable one for its UowService.
	/// </summary>
	/// <remarks>
	/// The client should set 'RollBack' to false, as the last instruction in a 'using' structure.
	/// That will allow the change(s) to be saved.
	/// If an exception is thrown, during the course of the chenge(s),
	/// then there is an automatic rollback, if 'RollBack' is not set to false at the end.
	/// </remarks>
	public sealed class NonUndoableUnitOfWorkHelper : UnitOfWorkHelper
	{
		private readonly IActionHandler m_originalHandler;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="actionHandler"></param>
		public NonUndoableUnitOfWorkHelper(IActionHandler actionHandler) :
			base(((UndoStack)actionHandler).UowService.NonUndoableStack)
		{
			m_originalHandler = actionHandler;
			((UndoStack)actionHandler).UowService.SetCurrentStack(m_actionHandler);

			m_actionHandler.BeginNonUndoableTask();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the specified task, making it an non-undoable task in the action handler if
		/// necessary. If UOW has already been started, then this task will be done as part of
		/// the existing UOW.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void DoUsingNewOrCurrentUOW(IActionHandler actionHandler, Action task)
		{
			var uowService = ((UndoStack)actionHandler).UowService;
			if (uowService.CurrentProcessingState == UnitOfWorkService.FdoBusinessTransactionState.ProcessingDataChanges)
				task();
			else
				Do(actionHandler, task);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the specified task, making it a non-undoable task in the action handler if
		/// necessary. If UOW has already been started, then this task will be done as part of
		/// the existing UOW. If we CANNOT start a UOW right now (e.g., we're in the PropChagned phase),
		/// on an end user machine quietly do nothing; but Debug.Fail so developers are warned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void DoUsingNewOrCurrentUowOrSkip(IActionHandler actionHandler, string debugMsg, Action task)
		{
			var uowService = ((UndoStack)actionHandler).UowService;
			if (uowService.CurrentProcessingState == UnitOfWorkService.FdoBusinessTransactionState.ProcessingDataChanges)
				task();
			else if (uowService.CurrentProcessingState == UnitOfWorkService.FdoBusinessTransactionState.ReadyForBeginTask)
				Do(actionHandler, task);
			else
				Debug.Fail(debugMsg);
		}

		/// <summary>
		/// Perform the specified task, making it a non-undoable task in the specified action handler.
		/// The task will automatically be begun and ended if all goes well, and rolled
		/// back if an exception is thrown; the exception will then be rethrown.
		/// </summary>
		public static void Do(IActionHandler actionHandler, Action task)
		{
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(actionHandler))
			{
				task();
				undoHelper.RollBack = false; // task ran successfully, don't roll back.
			}
		}

		/// <summary>
		/// Perform the specified task, making it a non-undoable task in the specified action handler.
		/// The task will automatically be begun and ended if all goes well, and rolled
		/// back if an exception is thrown; the exception will then be rethrown.
		/// </summary>
		public static T Do<T>(IActionHandler actionHandler, Func<T> task)
		{
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(actionHandler))
			{
				T retVal = task();
				undoHelper.RollBack = false; // task ran successfully, don't roll back.
				return retVal;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ends the undo task when not rolling back the changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void EndUndoTask()
		{
			m_actionHandler.EndNonUndoableTask();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does anything needing to be done after ending the undo task (rolling back or not).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void DoStuffAfterEndingTask()
		{
			UnitOfWorkService uowService = ((UndoStack)m_actionHandler).UowService;
			uowService.SetCurrentStack(m_originalHandler);
		}
	}
}