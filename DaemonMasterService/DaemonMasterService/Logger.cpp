//  DaemonMasterService: Logger
//  
//  This file is part of DeamonMasterService.
// 
//  DeamonMaster is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//   DeamonMaster is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with DeamonMasterService.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "Logger.h"
#include <spdlog/logger.h>
#include <spdlog/sinks/rotating_file_sink.h>

#define LOGFILE_PATH "logs/"
#define MAX_LOGFILE_SIZE (1024 * 1024 * 5)
#define LOGFILE_COUNT 3

using namespace std;


static shared_ptr<spdlog::logger> logger = nullptr;

Logger::Logger(const std::string& logName)
{
	//prevent multiinstances
	if (logger) { return; }

	//Init spdlog
	try
	{
		// Create a file rotating logger with 5mb size max and 3 rotated files
		logger = spdlog::rotating_logger_mt(logName, LOGFILE_PATH + logName + ".log", MAX_LOGFILE_SIZE, LOGFILE_COUNT);
		logger->info("SPDLOG: Initialisation was successful");
		std::cout << "SPDLOG: Initialisation was successful" << std::endl;
	}
	catch (const spdlog::spdlog_ex &ex)
	{
		std::cout << "SPDLOG: Initialization failed (logging system has been disabled): " << ex.what() << std::endl;
	}
}

Logger::~Logger()
{
	if (logger)
	{
		// Under VisualStudio, this must be called before main finishes to workaround a known VS issue
		spdlog::drop_all();
	}
}

void Logger::Debug(const string& msg)
{
	if (!logger) { return; }
	logger->debug(msg);
}

void Logger::Info(const string& msg)
{
	if (!logger) { return; }
	logger->info(msg);
}

void Logger::Error(const string& msg)
{
	if (!logger) { return; }
	logger->error(msg);
}

void Logger::Critical(const string& msg)
{
	if (!logger) { return; }
	logger->critical(msg);
}