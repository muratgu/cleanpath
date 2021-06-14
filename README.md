# cleanpath

Windows Path Cleaner
====================

Trims either the user or the machine path, by removing duplicates or non-existent directories from the path string and by replacing the long names with the short versions.

Requires administrator rights to be able to modify the registry entries related to the user and machine path settings.

Usage:

	cleanpath 
		-m, --machine : Reports duplicate/obsolete/long paths in the machine path. Default is user path.
		-l, --list    : Lists current path strings
		-f, --full    : Lists full path strings
		-c, --change  : Prompt for confirmation and change path if needed
		-y, --yes     : Respond Y to confirmation prompt




